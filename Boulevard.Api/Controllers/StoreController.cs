using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

[ApiController]
[Route("api/v1/[controller]")]
public class StoreController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly string _uploadsFolder;
    private readonly ActivitySource _activitySource;

    public StoreController(AppDbContext context)
    {
        _context = context;
        _activitySource = new ActivitySource("Boulevard.Api"); // OpenTelemetry
        _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    }

    /// <summary>
    /// Crea una tienda y asigna el usuario actual como propietario.
    /// </summary>
    /// <remarks> Requiere autenticación. </remarks>
    /// <param name="request"> Datos de la tienda a crear. </param>
    /// <returns> La tienda creada. </returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateStore([FromForm] CreateStoreRequest request)
    {
        Console.WriteLine("CreateStore called");  // Add this line
        using var activity = _activitySource.StartActivity("CreateStore");
        activity?.SetTag("request.storeName", request.Name);

        Console.WriteLine($"User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}"))}");

        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "User sub claim is missing");
            return Unauthorized("User sub claim is missing");
        }

        try
        {
            string? logoPath = null;

            if (request.Logo != null)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.Logo.FileName);
                var filePath = Path.Combine(_uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Logo.CopyToAsync(stream);
                }

                logoPath = $"/uploads/{fileName}"; // Ruta servida como estática
            }

            var store = new Store
            {
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                City = request.City,
                Municipality = request.Municipality,
                Province = request.Province,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                LogoPath = logoPath ?? string.Empty,
            };

            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            UserStore userStoreRel = new UserStore
            {
                UserId = Guid.Parse(sub),
                StoreId = store.Id,
                Role = UserRole.Owner
            };

            _context.UserStores.Add(userStoreRel);
            await _context.SaveChangesAsync();

            activity?.SetStatus(ActivityStatusCode.Ok);

            return CreatedAtAction(nameof(GetById), new {id = store.Id}, store);
        }

        catch(Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return StatusCode(500, "Error interno al procesar la solicitud");
        }
    }

    /// <summary>
    /// Obtiene una tienda por su ID.
    /// Consulta pública.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var store = await _context.Stores.FindAsync(id);

        if(store == null)
            return NotFound();
        
        return Ok(store);
    }

    /// <summary>
    /// Actualiza una tienda existente.
    /// Requiere autenticación.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStore(Guid id, [FromForm] UpdateStoreRequest request)
    {
        var store = await _context.Stores.FindAsync(id);

        if(store == null)
            return NotFound();
        
        // Obtiene el ID del usuario actual desde el token
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Verifica si el usuario actual es el propietario de la tienda
        var userStore = await _context.UserStores.FirstOrDefaultAsync(us => us.UserId == userId && us.StoreId == id);
        if(userStore == null || userStore.Role != UserRole.Owner && userStore.Role != UserRole.Manager)
            return Unauthorized("No tienes permiso para modificar esta tienda.");

        store.Name = request.Name ?? store.Name;
        store.Description = request.Description ?? store.Description;
        store.Address = request.Address ?? store.Address;
        store.City = request.City ?? store.City;
        store.Municipality = request.Municipality ?? store.Municipality;
        store.Province = request.Province ?? store.Province;
        store.PhoneNumber = request.PhoneNumber ?? store.PhoneNumber;
        store.Email = request.Email ?? store.Email;

        _context.Stores.Update(store);
        await _context.SaveChangesAsync();

        return Ok(store);
    }

    /// <summary>
    /// Elimina una tienda por su ID.
    /// Requiere autenticación.
    /// Requiere que el usuario sea el dueño de la tienda.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")] 
    [Authorize]
    public async Task<IActionResult> DeleteStore(Guid id)
    {
        var store = await _context.Stores.FindAsync(id);
        if(store == null)
            return NotFound();

        // Obtiene el ID del usuario actual desde el token
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Verifica si el usuario actual es el propietario de la tienda
        var userStore = await _context.UserStores.FirstOrDefaultAsync(us => us.UserId == userId && us.StoreId == id);
        if(userStore == null || userStore.Role != UserRole.Owner)
            return Unauthorized("No tienes permiso para eliminar esta tienda.");
        
        _context.Stores.Remove(store);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Añade un trabajador a la tienda.
    /// Requiere permisos de propietario para añadir un administrador u otro propietario
    /// Requiere permisos de administrador para añadir un vendedor o repartidor
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("{id}/workers")]
    [Authorize]
    public async Task<IActionResult> AddWorker(Guid id, [FromForm] AddWorkerRequest request)
    {
        var store = await _context.Stores.FindAsync(id);
        if(store == null)
            return NotFound();
        
        // Obtiene el ID del usuario actual desde el token
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Verifica en la petición el rol del usuario a añadir. 
        // Si es administrador o propietario, solo puede añadirlo un propietario
        // De si es vendedor o repartidor, solo puede añadirlo un propietario o administrador
        if(request.Role == UserRole.Owner || request.Role == UserRole.Manager)
        {
            var userStorePerformer = await _context.UserStores.FirstOrDefaultAsync(us => us.UserId == userId && us.StoreId == id);
            if(userStorePerformer == null || userStorePerformer.Role != UserRole.Owner)
                return Unauthorized("No tienes permiso para añadir un administrador o propietario a esta tienda.");
        }
        else if(request.Role == UserRole.Salesperson || request.Role == UserRole.Delivery)
        {
            var userStorePerformer = await _context.UserStores.FirstOrDefaultAsync(us => us.UserId == userId && us.StoreId == id);
            if(userStorePerformer == null || (userStorePerformer.Role != UserRole.Owner && 
                                userStorePerformer.Role != UserRole.Manager))
                return Unauthorized("No tienes permiso para añadir un trabajador a esta tienda.");
        }

        // Verifica que el usuario a añadir exista
        var userToAdd = await _context.Users.FindAsync(request.UserId);
        if(userToAdd == null)
            return BadRequest("El usuario aañadir no existe.");
        
        // Verifica que el usuario a añadir no esté ya asignado a la tienda
        var existingRelation = await _context.UserStores.FirstOrDefaultAsync(us => us.UserId == request.UserId &&
                        us.StoreId == id);
        if(existingRelation != null)
            return BadRequest("El usuario ya está asignado a esta tienda.");

        var userStore = new UserStore
        {
            UserId = request.UserId,
            StoreId = id,
            Role = request.Role
        };

        _context.UserStores.Add(userStore);
        await _context.SaveChangesAsync();

        return Ok(userStore);
    }

    [HttpDelete("{id}/workers/{workerId}")] 
    [Authorize]
    public async Task<IActionResult> RemoveWorker(Guid id, Guid workerId)
    {
        var store = await _context.Stores.FindAsync(id);
        if(store == null)
            return NotFound();
        
        // Obtiene el ID del usuario actual desde el token
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);


        // Verifica si el trabajador existe en la tienda
        var userStoreToRemove = await _context.UserStores.FirstOrDefaultAsync(us => us.UserId == workerId && us.StoreId == id);
        if(userStoreToRemove == null)
            return BadRequest("El trabajador no existe en esta tienda.");
        
        // Verifica el rol del usuario a eliminar
        // Si es administrador o propietario, solo puede eliminarlo un propietario
        // Si es vendedor o repartidor, solo puede eliminarlo un propietario o administrador
        if(userStoreToRemove.Role == UserRole.Owner || userStoreToRemove.Role == UserRole.Manager)
        {
            var userStorePerformer = await _context.UserStores.FirstOrDefaultAsync(us => us.UserId == userId && us.StoreId == id);
            if(userStorePerformer == null || userStorePerformer.Role != UserRole.Owner)
                return Unauthorized("No tienes permiso para eliminar un administrador o propietario de esta tienda.");
        }
        else if(userStoreToRemove.Role == UserRole.Salesperson || userStoreToRemove.Role == UserRole.Delivery)
        {
            var userStorePerformer = await _context.UserStores.FirstOrDefaultAsync(us => us.UserId == userId && us.StoreId == id);
            if(userStorePerformer == null || (userStorePerformer.Role != UserRole.Owner && 
                                userStorePerformer.Role != UserRole.Manager))
                return Unauthorized("No tienes permiso para eliminar un trabajador de esta tienda.");
        }
        
        _context.UserStores.Remove(userStoreToRemove);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}