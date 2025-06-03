using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using monk_mode_backend.Domain;
using monk_mode_backend.Infrastructure;
using monk_mode_backend.Models;

namespace monk_mode_backend.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DailyStatisticsController : ControllerBase {
        private readonly MonkModeDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public DailyStatisticsController(MonkModeDbContext context, UserManager<ApplicationUser> userManager, IMapper mapper) {
            _dbContext = context;
            _userManager = userManager;
            _mapper = mapper;
        }

        // GET /daily-statistics
        [HttpGet]
        public async Task<IActionResult> GetDailyStatistics([FromQuery] DateTime? date) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var query = _dbContext.DailyStatistics
                .Where(ds => ds.UserId == user.Id);

            if (date.HasValue) {
                var startOfDay = date.Value.Date;
                var endOfDay = startOfDay.AddDays(1);
                query = query.Where(ds => ds.Date >= startOfDay && ds.Date < endOfDay);
            }

            var statistics = await query.ToListAsync();
            var statisticsDTOs = _mapper.Map<List<DailyStatisticsDTO>>(statistics);

            return Ok(statisticsDTOs);
        }

        // POST /daily-statistics/update
        [HttpPost("update")]
        public async Task<IActionResult> UpdateDailyStatistics([FromBody] DailyStatisticsDTO statisticsData) {
            if (statisticsData == null)
                return BadRequest("Invalid statistics data.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var startOfDay = statisticsData.Date.Date;
            var endOfDay = startOfDay.AddDays(1);

            var existingStats = await _dbContext.DailyStatistics
                .FirstOrDefaultAsync(ds => ds.UserId == user.Id && ds.Date >= startOfDay && ds.Date < endOfDay);

            if (existingStats == null) {
                // Create new statistics for the day
                var newStats = _mapper.Map<DailyStatistics>(statisticsData);
                newStats.UserId = user.Id;
                _dbContext.DailyStatistics.Add(newStats);
            } else {
                // Add to existing statistics instead of overwriting
                existingStats.TotalFocusTime += statisticsData.TotalFocusTime;
            }

            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }
} 