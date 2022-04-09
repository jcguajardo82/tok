using System.Threading.Tasks;
using Soriana.PPS.Common.DTO.Card;

namespace Soriana.PPS.Card.SorianaApp.Services
{
    public interface ISorianaAppService
    {
        Task<AddCardResponse> AddCard(AddCardSorianaAppRequest entity, string SalesForceRequest);
        Task<AddCardResponse> UpdatePredeterminatedCard(AddCardSorianaAppRequest entity);
    }
}
