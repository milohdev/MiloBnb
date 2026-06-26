using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Milo.Domain.Entities;
using Milo.Domain.Entities.Enums;

namespace Milo.Infraestructure.Persistence.Seeders;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var context = services.GetRequiredService<MiloDbContext>();

        if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        var hasher = new PasswordHasher<User>();

        // Admin
        var adminEmail = configuration["Admin:Email"]!;
        var adminPassword = configuration["Admin:Password"]!;
        var adminTemp = User.Create("Admin", "MiloBnb", adminEmail, string.Empty, UserRole.Admin);
        var admin = User.Create("Admin", "MiloBnb", adminEmail,
            hasher.HashPassword(adminTemp, adminPassword), UserRole.Admin);
        context.Users.Add(admin);

        // Owner de prueba
        var ownerTemp = User.Create("Carlos", "Restrepo", "owner@test.com", string.Empty, UserRole.Owner);
        var owner = User.Create("Carlos", "Restrepo", "owner@test.com",
            hasher.HashPassword(ownerTemp, "Password123"), UserRole.Owner);
        context.Users.Add(owner);

        // Guest de prueba con KYC verificado
        var guestTemp = User.Create("Laura", "Gómez", "guest@test.com", string.Empty, UserRole.Guest);
        var guest = User.Create("Laura", "Gómez", "guest@test.com",
            hasher.HashPassword(guestTemp, "Password123"), UserRole.Guest);
        guest.MarkKycVerified();
        context.Users.Add(guest);

        await context.SaveChangesAsync();

        // Propiedades
        var apt = Property.Create(
            name: "Apartamento moderno en El Poblado",
            description: "Amplio apartamento de 2 habitaciones en el corazón de El Poblado, el barrio más vibrante de Medellín. A pasos de restaurantes, cafés y vida nocturna. Con terraza privada y vista a la montaña.",
            address: "Calle 10 # 38-15, El Poblado",
            city: "Medellín",
            country: "Colombia",
            pricePerNight: 150_000,
            maxGuests: 4,
            bedrooms: 2,
            bathrooms: 1,
            allowSameDayBooking: true,
            ownerId: owner.Id);

        var house = Property.Create(
            name: "Casa colonial frente al mar en Bocagrande",
            description: "Hermosa casa estilo colonial a 50 metros de la playa de Bocagrande. Perfecta para familias o grupos. Cuenta con piscina privada, cocina totalmente equipada y acceso directo al malecón.",
            address: "Avenida San Martín # 6-81, Bocagrande",
            city: "Cartagena",
            country: "Colombia",
            pricePerNight: 300_000,
            maxGuests: 8,
            bedrooms: 4,
            bathrooms: 3,
            allowSameDayBooking: false,
            ownerId: owner.Id);

        var cabin = Property.Create(
            name: "Cabaña ecológica con vista al Peñol",
            description: "Cabaña rústica y acogedora con vista privilegiada al Peñol de Guatapé. Rodeada de naturaleza, ideal para desconectarse. Incluye fogón, hamacas y acceso a sendero privado hasta el embalse.",
            address: "Vereda El Tablazo, Km 3 vía Guatapé",
            city: "Guatapé",
            country: "Colombia",
            pricePerNight: 200_000,
            maxGuests: 6,
            bedrooms: 3,
            bathrooms: 2,
            allowSameDayBooking: true,
            ownerId: owner.Id);

        context.Properties.AddRange(apt, house, cabin);
        await context.SaveChangesAsync();

        // Imágenes
        context.Set<PropertyImage>().AddRange(
            PropertyImage.Create(apt.Id, "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&auto=format&fit=crop"),
            PropertyImage.Create(apt.Id, "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800&auto=format&fit=crop"),
            PropertyImage.Create(house.Id, "https://images.unsplash.com/photo-1499793983690-e29da59ef1c2?w=800&auto=format&fit=crop"),
            PropertyImage.Create(house.Id, "https://images.unsplash.com/photo-1564013799919-ab600027ffc6?w=800&auto=format&fit=crop"),
            PropertyImage.Create(cabin.Id, "https://images.unsplash.com/photo-1449158743715-0a90ebb6d2d8?w=800&auto=format&fit=crop"),
            PropertyImage.Create(cabin.Id, "https://images.unsplash.com/photo-1510798831971-661eb04b3739?w=800&auto=format&fit=crop"));

        await context.SaveChangesAsync();

        // Reserva confirmada del Guest en el apartamento
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-3));
        var checkOut = checkIn.AddDays(5);
        var reservation = Reservation.Create(apt.Id, guest.Id, checkIn, checkOut, apt.PricePerNight);
        context.Reservations.Add(reservation);

        await context.SaveChangesAsync();
    }
}
