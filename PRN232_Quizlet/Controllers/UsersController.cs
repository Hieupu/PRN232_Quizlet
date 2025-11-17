using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PRN232_Quizlet.DTOs;
using PRN232_Quizlet.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PRN232_Quizlet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly Prn232QuizletContext _context;
        private readonly IConfiguration _config;

        public UsersController(Prn232QuizletContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("Register")]
        public IActionResult Register([FromBody] UserRegister model)
        {
            if (_context.Users.Any(u => u.Email == model.Email))
                return BadRequest("Email already exists.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var user = new User
            {
                Email = model.Email,
                PasswordHash = hashedPassword,
                FullName = model.FullName,
                Role = "User",
                Status = "Active",
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { message = "Registered successfully!" });
        }

        [HttpPost("Login")]
        public IActionResult Login([FromBody] UserLogin login)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == login.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
                return Unauthorized("Invalid username or password.");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login successful",
                token,
                user = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.Role
                }
            });
        }

        [Authorize]
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                message = "You are authorized!",
                user = new { email, name, role }
            });
        }

        /// <summary>
        /// UC03: Cập nhật thông tin cá nhân - Thay đổi tên hiển thị, mật khẩu
        /// </summary>
        /// <param name="request">Thông tin cần cập nhật (FullName và/hoặc Password)</param>
        /// <returns>Thông tin user sau khi cập nhật</returns>
        [Authorize]
        [HttpPut("profile")]
        public IActionResult UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            // Bước 1: Validate request body
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required." });
            }

            // Bước 2: Kiểm tra ít nhất phải có 1 trường để cập nhật
            if (string.IsNullOrWhiteSpace(request.FullName) && string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "At least one field (FullName or Password) must be provided." });
            }

            // Bước 3: Lấy UserID từ JWT token
            var userIdClaim = User.FindFirstValue("UserID");
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            // Bước 4: Tìm user trong database
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Bước 5: Cập nhật FullName (nếu có)
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                var fullName = request.FullName.Trim();
                if (string.IsNullOrEmpty(fullName))
                {
                    return BadRequest(new { message = "FullName cannot be empty." });
                }
                user.FullName = fullName;
            }

            // Bước 6: Cập nhật Password (nếu có)
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                // Kiểm tra CurrentPassword bắt buộc khi đổi password
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                {
                    return BadRequest(new { message = "Current password is required to change password." });
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Current password is incorrect." });
                }

                // Hash password mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            // Bước 7: Lưu thay đổi vào database
            _context.SaveChanges();

            // Bước 8: Trả về thông tin user đã được cập nhật
            return Ok(new
            {
                message = "Profile updated successfully.",
                user = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.Role
                }
            });
        }

        /// <summary>
        /// UC04: Đăng xuất - Xóa token và kết thúc phiên làm việc
        /// </summary>
        /// <returns>Message xác nhận logout thành công</returns>
        [Authorize]
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                message = "Logout successful",
                user = new { email, name, role }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserID", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpireMinutes"] ?? "60")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
