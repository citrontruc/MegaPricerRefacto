using MegaPricer.Dtos;

namespace MegaPricer.Data;

public interface IUserMarkupRepository
{
    public Task<IEnumerable<UserMarkupDto>> RetrieveUserMarkupAsync(string userName);
}
