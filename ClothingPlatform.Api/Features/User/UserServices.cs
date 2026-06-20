using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Api.Models.Order;
using ClothingPlatform.Api.Models.Product;
using ClothingPlatform.Api.Models.Staff;
using ClothingPlatform.Api.Models.User;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.Api.Features.User
{
    public class UserServices : IUserService
    {
        private readonly AppDbContext _db;

        public UserServices(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<UserModel>> GetUsersCustomerAsync(int page, int pageSize)
        {
            return await GetUsersByRoleAsync("customer", page, pageSize);
        }

        public async Task<PagedResult<UserModel>> GetUsersStaffAsync(int page, int pageSize)
        {
            return await GetUsersByRoleAsync("staff", page, pageSize);
        }

        public UserDto? GetUserDto(int userId)
        {
            var user = _db.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .FirstOrDefault(x => x.UserId == userId);
            if (user == null)
            {
                return null;
            }

            return new UserDto
            {
                Id = userId,
                First_Name = user.FirstName,
                Last_Name = user.LastName,
                Address = user.Address,
                Email = user.Email
            };
        }

        public void CreateUser(CreatRquestModel userdto)
        {
            var role = _db.Roles.FirstOrDefault(r => r.RoleName == userdto.Role);
            if (role == null)
            {
                throw new InvalidOperationException($"Role '{userdto.Role}' does not exist.");
            }

            var user = new ClothingPlatform.DB.AppDbModels.User
            {
                FirstName = userdto.First_Name,
                LastName = userdto.Last_Name,
                Address = userdto.Address,
                Email = userdto.Email.Trim().ToLowerInvariant(),
                PhoneNumber = string.Empty,
                PasswordHash = string.Empty,
                RoleId = role.RoleId,
                CreatedAt = DateTime.Now
            };
            _db.Users.Add(user);
            _db.SaveChanges();
        }

        public async Task<StaffDashboardDto> GetDashboardAsync()
        {
            var orders = await _db.Orders
                .Include(x => x.User)
                .Include(x => x.Payments)
                .Include(x => x.OrderItems)
                .OrderByDescending(x => x.OrderId)
                .Select(x => new OrderDto
                {
                    OrderId = x.OrderId,
                    CustomerName = x.User.FirstName + " " + x.User.LastName,
                    TotalAmount = x.TotalAmount,
                    OrderStatus = x.OrderStatus,
                    PaymentMethod = x.Payments.Select(p => p.PaymentMethod).FirstOrDefault() ?? "COD",
                    ItemCount = x.OrderItems.Count,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            var inventory = await _db.ProductVariants
                .Include(x => x.Product)
                .Select(x => new InventoryDto
                {
                    VariantId = x.VariantId,
                    Sku = x.Sku,
                    ProductName = x.Product.Name,
                    Size = x.Size,
                    Color = x.Color,
                    StockQuantity = x.StockQuantity
                })
                .ToListAsync();

            return new StaffDashboardDto
            {
                Orders = orders,
                Inventory = inventory
            };
        }

        public void UpdateUser(int userId, UpdateRequestModel model)
        {
            var item = _db.Users
                .Include(x => x.Role)
                .FirstOrDefault(x => x.UserId == userId);
            if (item == null)
            {
                return;
            }

            item.FirstName = model.First_Name;
            item.LastName = model.Last_Name;
            item.Address = model.Address;
            item.Email = model.Email.Trim().ToLowerInvariant();

            var role = _db.Roles.FirstOrDefault(r => r.RoleName == model.Role);
            if (role != null)
            {
                item.RoleId = role.RoleId;
            }

            _db.SaveChanges();
        }

        public void DeleteUser(int userId)
        {
            var item = _db.Users.FirstOrDefault(x => x.UserId == userId);
            if (item == null) { return; }
            _db.Users.Remove(item);
            _db.SaveChanges();
        }

        private async Task<PagedResult<UserModel>> GetUsersByRoleAsync(string roleName, int page, int pageSize)
        {
            var query = _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == roleName);

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserModel
                {
                    Id = u.UserId,
                    First_Name = u.FirstName,
                    Last_Name = u.LastName,
                    Email = u.Email,
                    Address = u.Address,
                    Role = u.Role.RoleName,
                    PhoneNo = u.PhoneNumber,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<UserModel>
            {
                Items = users,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
