using MegaPricer.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MegaPricer.Data;

public class UserMarkupRepository : IUserMarkupRepository
{
    private ApplicationDbContext _context;
    public UserMarkupRepository(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }

    public async Task<IEnumerable<UserMarkupDto>> RetrieveUserMarkupAsync(string userName)
    {
        return await _context.UserMarkups
            .AsNoTracking()
            .Where(x => x.UserName == userName)
            .Select(
            x => new UserMarkupDto()
                {
                    UserName = x.UserName,
                    UserMarkup = x.MarkupPercent
                }
            ).ToListAsync();
    }
}
