using System;
using System.Linq;
using System.Threading.Tasks;
using HotelManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HotelManagement.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // 1) Роли
            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2) Admin потребител
            string adminEmail = "admin@hotel.local";
            string adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // НЯМА админ → създаваме нов
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);

                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                // (по желание: логване на грешките)
            }
            else
            {
                // ИМА админ → УБЕЖДАВАМЕ СЕ, че е настроен правилно

                bool changed = false;

                if (!string.Equals(adminUser.UserName, adminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    adminUser.UserName = adminEmail;
                    adminUser.NormalizedUserName = adminEmail.ToUpperInvariant();
                    changed = true;
                }

                if (!adminUser.EmailConfirmed)
                {
                    adminUser.EmailConfirmed = true;
                    changed = true;
                }

                if (changed)
                {
                    await userManager.UpdateAsync(adminUser);
                }

                // Убеждаваме се, че е в роля Admin
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
