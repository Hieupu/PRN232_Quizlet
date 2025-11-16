using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_Quizlet.DTOs;
using PRN232_Quizlet.Models;
using System.Security.Claims;

namespace PRN232_Quizlet.Controllers
{
    /// UC01: Xem danh sách người dùng
    /// UC02: Cập nhật / khóa tài khoản
    /// UC03: Xem danh sách bộ thẻ học
    /// UC04: Duyệt / xóa nội dung vi phạm
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly Prn232QuizletContext _context;

        private const int MAX_PAGE_SIZE = 100;

        public AdminController(Prn232QuizletContext context)
        {
            _context = context;
        }
        private IActionResult? CheckAdminPermission()
        {
            // Lấy Role từ token JWT
            var roleClaim = User?.FindFirst(ClaimTypes.Role)?.Value;
            // Nếu không có token hoặc Role không phải "Admin" → từ chối
            if (string.IsNullOrEmpty(roleClaim) || roleClaim != "Admin")
            {
                // Không có token → yêu cầu đăng nhập (401)
                if (roleClaim == null)
                {
                    return Unauthorized(new { message = "Authentication required." });
                }
                // Có token nhưng không phải Admin → không có quyền (403)
                else
                {
                    return Forbid("Access denied. Admin role required.");
                }
            }

            // Có quyền Admin → cho phép tiếp tục
            return null;
        }
        private int? GetCurrentUserId()
        {
            // Lấy claim "UserID" từ token
            var userIdClaim = User?.FindFirst("UserID")?.Value;
            // Parse string thành int, nếu lỗi thì trả về null
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// UC01: Xem danh sách người dùng - Hiển thị tất cả tài khoản trong hệ thống
        /// </summary>
        /// <param name="search">Tìm kiếm theo email hoặc tên đầy đủ</param>
        /// <param name="role">Lọc theo vai trò (Admin/User)</param>
        /// <param name="status">Lọc theo trạng thái (Active/Inactive)</param>
        /// <param name="page">Số trang (bắt đầu từ 1)</param>
        /// <param name="pageSize">Số bản ghi trên 1 trang</param>
        /// <returns>Danh sách users với phân trang</returns>
        [HttpGet("users")]
        public IActionResult GetAllUsers(
            string? search = "", 
            string? role = "", 
            string? status = "", 
            int page = 1, 
            int pageSize = 10)
        {
            // Bước 1: Kiểm tra quyền Admin
            var permissionCheck = CheckAdminPermission();
            if (permissionCheck != null)
            {
                return permissionCheck; // Trả về lỗi nếu không có quyền
            }
            // Bước 2: Validate và chuẩn hóa tham số phân trang
            // Đảm bảo page >= 1
            if (page < 1)
            {
                page = 1;
            }
            // Đảm bảo pageSize hợp lệ (1 <= pageSize <= MAX_PAGE_SIZE)
            if (pageSize < 1)
            {
                pageSize = 10; // Mặc định 10 bản ghi/trang
            }
            if (pageSize > MAX_PAGE_SIZE)
            {
                pageSize = MAX_PAGE_SIZE; // Giới hạn tối đa
            }

            // Bước 3: Tạo query để lấy danh sách users
            var query = _context.Users.AsQueryable();

            // Bước 4: Áp dụng các filter tìm kiếm

            // Filter 1: Tìm kiếm theo email hoặc tên (không phân biệt hoa thường)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower(); // Chuyển sang chữ thường để so sánh
                query = query.Where(u => 
                    u.Email.ToLower().Contains(searchLower) ||      // Tìm trong email
                    u.FullName.ToLower().Contains(searchLower));    // Tìm trong tên đầy đủ
            }

            // Filter 2: Lọc theo vai trò (Role)
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role.Trim());
            }

            // Filter 3: Lọc theo trạng thái (Status)
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.Status == status.Trim());
            }

            // Bước 5: Đếm tổng số bản ghi sau khi filter (trước khi phân trang)
            var total = query.Count();

            // Bước 6: Áp dụng phân trang và sắp xếp
            var users = query
                .OrderByDescending(u => u.CreatedAt)              // Sắp xếp mới nhất trước
                .Skip((page - 1) * pageSize)                       // Bỏ qua các bản ghi ở trang trước
                .Take(pageSize)                                    // Lấy đúng số bản ghi của trang hiện tại
                .Select(u => new                                   // Chỉ lấy các trường cần thiết
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.Status,
                    u.CreatedAt
                })
                .ToList();

            // Bước 7: Tính tổng số trang
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // Bước 8: Trả về kết quả với thông tin phân trang
            return Ok(new
            {
                total,              // Tổng số bản ghi
                page,               // Trang hiện tại
                pageSize,           // Số bản ghi/trang
                totalPages,         // Tổng số trang
                data = users        // Danh sách users
            });
        }
        /// <param name="id">ID của user cần cập nhật</param>
        /// <param name="request">Thông tin cần cập nhật (Role và/hoặc Status)</param>
        /// <returns>Thông tin user sau khi cập nhật</returns>
        [HttpPut("users/{id}")]
        public IActionResult UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            // Bước 1: Kiểm tra quyền Admin
            var permissionCheck = CheckAdminPermission();
            if (permissionCheck != null)
            {
                return permissionCheck; // Trả về lỗi nếu không có quyền
            }

            // Bước 2: Validate request body
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required." });
            }

            // Bước 3: Kiểm tra ít nhất phải có 1 trường để cập nhật (Role hoặc Status)
            if (string.IsNullOrWhiteSpace(request.Role) && string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { message = "At least one field (Role or Status) must be provided." });
            }

            // Bước 4: Tìm user theo ID
            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Bước 5: Kiểm tra Admin không thể tự khóa chính mình
            var currentUserId = GetCurrentUserId();
            bool isTryingToLockOwnAccount = currentUserId == id 
                && !string.IsNullOrWhiteSpace(request.Status) 
                && request.Status.Trim() == "Inactive";

            if (isTryingToLockOwnAccount)
            {
                return BadRequest(new { message = "You cannot lock your own account." });
            }

            // Bước 6: Validate và cập nhật Role (nếu có)
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var role = request.Role.Trim(); // Loại bỏ khoảng trắng

                // Kiểm tra Role hợp lệ (chỉ "Admin" hoặc "User")
                if (role != "Admin" && role != "User")
                {
                    return BadRequest(new { message = "Invalid role. Role must be 'Admin' or 'User'." });
                }

                // Cập nhật Role
                user.Role = role;
            }

            // Bước 7: Validate và cập nhật Status (nếu có)
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.Trim(); // Loại bỏ khoảng trắng

                // Kiểm tra Status hợp lệ (chỉ "Active" hoặc "Inactive")
                if (status != "Active" && status != "Inactive")
                {
                    return BadRequest(new { message = "Invalid status. Status must be 'Active' or 'Inactive'." });
                }

                // Cập nhật Status
                user.Status = status;
            }

            // Bước 8: Lưu thay đổi vào database
            _context.SaveChanges();

            // Bước 9: Trả về thông tin user đã được cập nhật
            return Ok(new
            {
                message = "User updated successfully.",
                user = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.Status,
                    user.CreatedAt      
                }
            });
        }

        /// <summary>
        /// UC03: Xem danh sách bộ thẻ học - Quản lý tất cả Flashcard Sets trong hệ thống
        /// </summary>
        /// <param name="search">Tìm kiếm theo tiêu đề (Title)</param>
        /// <param name="createdBy">Lọc theo UserID của người tạo</param>
        /// <param name="page">Số trang (bắt đầu từ 1)</param>
        /// <param name="pageSize">Số bản ghi trên 1 trang</param>
        /// <returns>Danh sách Flashcard Sets với phân trang</returns>
        [HttpGet("flashcardsets")]
        public IActionResult GetAllFlashcardSets(
            string? search = "",
            int? createdBy = null,
            int page = 1,
            int pageSize = 10)
        {
            // Bước 1: Kiểm tra quyền Admin
            var permissionCheck = CheckAdminPermission();
            if (permissionCheck != null)
            {
                return permissionCheck; // Trả về lỗi nếu không có quyền
            }

            // Bước 2: Validate và chuẩn hóa tham số phân trang
            // Đảm bảo page >= 1
            if (page < 1)
            {
                page = 1;
            }
            // Đảm bảo pageSize hợp lệ (1 <= pageSize <= MAX_PAGE_SIZE)
            if (pageSize < 1)
            {
                pageSize = 10; // Mặc định 10 bản ghi/trang
            }
            if (pageSize > MAX_PAGE_SIZE)
            {
                pageSize = MAX_PAGE_SIZE; // Giới hạn tối đa
            }

            // Bước 3: Tạo query để lấy danh sách Flashcard Sets
            var query = _context.FlashcardSets
                .Include(s => s.CreatedByNavigation)
                .Where(s => s.Status == "Active") // Chỉ hiển thị sets đang hoạt động
                .AsQueryable();

            // Bước 4: Áp dụng các filter tìm kiếm

            // Filter 1: Tìm kiếm theo Title (không phân biệt hoa thường)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower(); // Chuyển sang chữ thường để so sánh
                query = query.Where(s => s.Title.ToLower().Contains(searchLower));
            }

            // Filter 2: Lọc theo người tạo (CreatedBy)
            if (createdBy.HasValue)
            {
                query = query.Where(s => s.CreatedBy == createdBy.Value);
            }

            // Bước 5: Đếm tổng số bản ghi sau khi filter (trước khi phân trang)
            var total = query.Count();

            // Bước 6: Áp dụng phân trang và sắp xếp
            var sets = query
                .OrderByDescending(s => s.CreatedAt)              // Sắp xếp mới nhất trước
                .Skip((page - 1) * pageSize)                       // Bỏ qua các bản ghi ở trang trước
                .Take(pageSize)                                    // Lấy đúng số bản ghi của trang hiện tại
                .Select(s => new                                   // Chỉ lấy các trường cần thiết
                {
                    s.SetId,
                    s.Title,
                    s.Description,
                    s.StudyCount,
                    s.Status,
                    s.CreatedAt,
                    Author = s.CreatedByNavigation.FullName,
                    AuthorId = s.CreatedByNavigation.UserId
                })
                .ToList();

            // Bước 7: Tính tổng số trang
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            // Bước 8: Trả về kết quả với thông tin phân trang
            return Ok(new
            {
                total,              // Tổng số bản ghi
                page,               // Trang hiện tại
                pageSize,           // Số bản ghi/trang
                totalPages,         // Tổng số trang
                data = sets         // Danh sách Flashcard Sets
            });
        }

        /// <summary>
        /// UC04: Xóa FlashcardSet vi phạm - Admin có thể xóa Set của bất kỳ ai
        /// </summary>
        /// <param name="id">ID của FlashcardSet cần xóa</param>
        /// <returns>Message xác nhận xóa thành công</returns>
        [HttpDelete("flashcardsets/{id}")]
        public IActionResult DeleteFlashcardSet(int id)
        {
            // Bước 1: Kiểm tra quyền Admin
            var permissionCheck = CheckAdminPermission();
            if (permissionCheck != null)
            {
                return permissionCheck; // Trả về lỗi nếu không có quyền
            }

            // Bước 2: Tìm FlashcardSet theo ID (không cần check Status, Admin có thể xóa cả Inactive)
            var set = _context.FlashcardSets
                .Include(s => s.Flashcards) // Include Flashcards để xóa tất cả
                .FirstOrDefault(s => s.SetId == id);

            // Bước 3: Kiểm tra Set có tồn tại không
            if (set == null)
            {
                return NotFound(new { message = "FlashcardSet not found." });
            }

            // Bước 4: Set Status = "Inactive" cho Set (soft delete)
            set.Status = "Inactive";

            // Bước 5: Set Status = "Inactive" cho tất cả Flashcards trong Set
            foreach (var flashcard in set.Flashcards)
            {
                flashcard.Status = "Inactive";
            }

            // Bước 6: Lưu thay đổi vào database
            _context.SaveChanges();

            // Bước 7: Trả về message thành công
            return Ok(new { message = "FlashcardSet deleted successfully." });
        }

        /// <summary>
        /// UC04: Xóa Flashcard riêng lẻ vi phạm - Admin có thể xóa Flashcard của bất kỳ ai
        /// </summary>
        /// <param name="id">ID của Flashcard cần xóa</param>
        /// <returns>Message xác nhận xóa thành công</returns>
        [HttpDelete("flashcards/{id}")]
        public IActionResult DeleteFlashcard(int id)
        {
            // Bước 1: Kiểm tra quyền Admin
            var permissionCheck = CheckAdminPermission();
            if (permissionCheck != null)
            {
                return permissionCheck; // Trả về lỗi nếu không có quyền
            }

            // Bước 2: Tìm Flashcard theo ID (không cần check Status)
            var flashcard = _context.Flashcards.FirstOrDefault(f => f.FlashcardId == id);

            // Bước 3: Kiểm tra Flashcard có tồn tại không
            if (flashcard == null)
            {
                return NotFound(new { message = "Flashcard not found." });
            }

            // Bước 4: Set Status = "Inactive" cho Flashcard (soft delete)
            flashcard.Status = "Inactive";

            // Bước 5: Lưu thay đổi vào database
            _context.SaveChanges();

            // Bước 6: Trả về message thành công
            return Ok(new { message = "Flashcard deleted successfully." });
        }
    }
}

