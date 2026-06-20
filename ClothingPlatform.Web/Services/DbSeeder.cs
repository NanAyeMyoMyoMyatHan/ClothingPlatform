using System;
using System.Collections.Generic;
using System.Linq;
using ClothingPlatform.DB.AppDbModels;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.Web.Services
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext db)
        {
            // Apply any migrations if needed (if using EF core migrations)
            // db.Database.EnsureCreated();

            // 1. Seed RBAC roles and users
            var adminRole = EnsureRole(db, "admin", "Full administrator access");
            var staffRole = EnsureRole(db, "staff", "Staff operations access");
            var customerRole = EnsureRole(db, "customer", "Customer shopping account");

            EnsureSeedUser(db, "Admin", "User", "admin@boutique.com", "admin123", adminRole.RoleId, "No. 123, Luxury Ave, Yangon", "09252522525");
            EnsureSeedUser(db, "Thiri", "San", "staff@boutique.com", "staff123", staffRole.RoleId, "No. 456, Atelier Rd, Yangon", "09222333444");
            EnsureSeedUser(db, "Emily", "Watson", "emily@gmail.com", "12345678", customerRole.RoleId, "No. 789, Style Street, Yangon", "09999888777");

            // 2. Seed Categories
            if (!db.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "New Arrivals", Slug = "new-arrivals" },
                    new Category { Name = "Dresses", Slug = "dresses" },
                    new Category { Name = "Blouses", Slug = "blouses" }
                };
                db.Categories.AddRange(categories);
                db.SaveChanges();
            }

            // 3. Seed Products and Variants
            if (!db.Products.Any())
            {
                var newArrivals = db.Categories.First(c => c.Slug == "new-arrivals");
                var dresses = db.Categories.First(c => c.Slug == "dresses");
                var blouses = db.Categories.First(c => c.Slug == "blouses");

                var seedProducts = new List<(Product Prod, List<string> Sizes, List<string> Colors, string ImgUrl)>
                {
                    (
                        new Product
                        {
                            Name = "Botanical Bloom Wrap Dress",
                            Description = "A flowing wrap dress in lightweight chiffon adorned with hand-drawn botanical prints. The adjustable tie waist flatters every silhouette, while the romantic flutter sleeves add effortless femininity.",
                            BasePrice = 85000,
                            IsFeatured = true,
                            CategoryId = dresses.CategoryId,
                            CreatedAt = DateTime.Now
                        },
                        new List<string> { "XS", "S", "M", "L", "XL" },
                        new List<string> { "Blush Pink", "Ivory White", "Sage Green" },
                        "https://images.unsplash.com/photo-1594938298603-c8148c4b4a43?w=600&q=80"
                    ),
                    (
                        new Product
                        {
                            Name = "Petal Cascade Midi Dress",
                            Description = "Cascading layers of soft tulle form this dreamlike midi dress. Subtle floral embossing on the bodice elevates it from brunch to evening wear with equal grace.",
                            BasePrice = 92000,
                            IsFeatured = true,
                            CategoryId = dresses.CategoryId,
                            CreatedAt = DateTime.Now
                        },
                        new List<string> { "S", "M", "L", "XL" },
                        new List<string> { "Dusty Rose", "Midnight Black", "Ivory White" },
                        "https://images.unsplash.com/photo-1623520441888-1cc6ad2fc58d?w=600&q=80"
                    ),
                    (
                        new Product
                        {
                            Name = "Garden Reverie Slip Dress",
                            Description = "A silky slip dress with delicate floral spaghetti straps, perfect for warm Yangon evenings. Pair with a denim jacket or wear alone for an effortlessly chic look.",
                            BasePrice = 68000,
                            IsFeatured = true,
                            CategoryId = dresses.CategoryId,
                            CreatedAt = DateTime.Now
                        },
                        new List<string> { "XS", "S", "M", "L" },
                        new List<string> { "Blush Pink", "Champagne Gold", "Cobalt Blue" },
                        "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=600&q=80"
                    ),
                    (
                        new Product
                        {
                            Name = "Azure Bloom Maxi Dress",
                            Description = "Floor-sweeping maxi silhouette in premium crepe fabric with an all-over watercolor floral pattern. The V-neckline and empire waist create a timeless, elongating effect.",
                            BasePrice = 110000,
                            IsFeatured = true,
                            CategoryId = dresses.CategoryId,
                            CreatedAt = DateTime.Now
                        },
                        new List<string> { "S", "M", "L", "XL", "XXL" },
                        new List<string> { "Royal Blue", "Ivory White", "Wine Red" },
                        "https://images.unsplash.com/photo-1572804013309-59a88b7e92f1?w=600&q=80"
                    ),
                    (
                        new Product
                        {
                            Name = "Sheer Poetry Blouse",
                            Description = "A billowing organza blouse with hand-sewn floral appliqués at the collar. The sheer fabric layers beautifully over slip tanks or high-waisted trousers.",
                            BasePrice = 52000,
                            IsFeatured = false,
                            CategoryId = blouses.CategoryId,
                            CreatedAt = DateTime.Now
                        },
                        new List<string> { "XS", "S", "M", "L", "XL" },
                        new List<string> { "Pearl White", "Blush Pink", "Soft Lavender" },
                        "https://images.unsplash.com/photo-1585487000160-6ebcfceb0d03?w=600&q=80"
                    ),
                    (
                        new Product
                        {
                            Name = "Magnolia Satin Blouse",
                            Description = "Luxurious satin blouse with a magnolia-inspired button placket. The draped front creates a sophisticated silhouette ideal for professional or evening settings.",
                            BasePrice = 63000,
                            IsFeatured = false,
                            CategoryId = blouses.CategoryId,
                            CreatedAt = DateTime.Now
                        },
                        new List<string> { "S", "M", "L", "XL" },
                        new List<string> { "Ivory White", "Midnight Black", "Dusty Rose" },
                        "https://images.unsplash.com/photo-1564257631407-4deb1f99d992?w=600&q=80"
                    ),
                    (
                        new Product
                        {
                            Name = "Rosewater Ruffle Blouse",
                            Description = "Tiered ruffle detailing cascades down the front of this romantic chiffon blouse. Lightweight and breathable, it pairs effortlessly with everything from wide-leg pants to pencil skirts.",
                            BasePrice = 57000,
                            IsFeatured = false,
                            CategoryId = blouses.CategoryId,
                            CreatedAt = DateTime.Now
                        },
                        new List<string> { "XS", "S", "M", "L", "XL" },
                        new List<string> { "Blush Pink", "Champagne Gold", "Sage Green" },
                        "https://images.unsplash.com/photo-1496747611176-843222e1e57c?w=600&q=80"
                    ),
                    (
                        new Product
                        {
                            Name = "Floral Reverie Shift Dress",
                            Description = "A structured shift dress in premium cotton-blend fabric with an elegant floral jacquard pattern. The clean A-line silhouette is both modern and universally flattering.",
                            BasePrice = 78000,
                            IsFeatured = false,
                            CategoryId = dresses.CategoryId,
                            CreatedAt = DateTime.Now
                        },
                        new List<string> { "S", "M", "L", "XL" },
                        new List<string> { "Cobalt Blue", "Wine Red", "Ivory White" },
                        "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=600&q=80"
                    )
                };

                int skuCounter = 1000;
                foreach (var item in seedProducts)
                {
                    db.Products.Add(item.Prod);
                    db.SaveChanges(); // to get ProductId

                    // Add primary image link
                    db.ProductImages.Add(new ProductImage
                    {
                        ProductId = item.Prod.ProductId,
                        ImageUrl = item.ImgUrl,
                        IsPrimary = true
                    });

                    // Add variants for every size/color combination
                    foreach (var size in item.Sizes)
                    {
                        foreach (var color in item.Colors)
                        {
                            skuCounter++;
                            db.ProductVariants.Add(new ProductVariant
                            {
                                ProductId = item.Prod.ProductId,
                                Size = size,
                                Color = color,
                                Sku = $"{item.Prod.Name.Substring(0, 3).ToUpper()}-{size}-{color.Replace(" ", "").ToUpper()}-{skuCounter}",
                                StockQuantity = 25,
                                PriceModifier = 0.00m
                            });
                        }
                    }
                }
                db.SaveChanges();
            }
        }

        private static Role EnsureRole(AppDbContext db, string roleName, string description)
        {
            var role = db.Roles.FirstOrDefault(r => r.RoleName == roleName);
            if (role != null) return role;

            role = new Role
            {
                RoleName = roleName,
                Description = description,
                CreatedAt = DateTime.Now
            };
            db.Roles.Add(role);
            db.SaveChanges();
            return role;
        }

        private static void EnsureSeedUser(
            AppDbContext db,
            string firstName,
            string lastName,
            string email,
            string password,
            int roleId,
            string address,
            string phoneNumber)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = db.Users.FirstOrDefault(u => u.Email.ToLower() == normalizedEmail);
            if (user != null)
            {
                if (user.RoleId != roleId)
                {
                    user.RoleId = roleId;
                    db.SaveChanges();
                }

                return;
            }

            db.Users.Add(new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId = roleId,
                Address = address,
                PhoneNumber = phoneNumber,
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();
        }
    }
}
