using CourtListenerMcpServer.Models;

namespace CourtListenerMcpServer.Services;

public interface ICourtListenerClient
{
    Task<OpinionSearchResult> SearchOpinionsAsync(Dictionary<string, object> parameters);
    Task<Opinion> GetOpinionAsync(int opinionId);
    Task<DocketSearchResult> SearchDocketsAsync(Dictionary<string, object> parameters);
    Task<Docket> GetDocketAsync(int docketId);
    Task<CourtSearchResult> SearchCourtsAsync(Dictionary<string, object> parameters);
    Task<Court> GetCourtAsync(string courtId);
}
