using Soriana.PPS.Common.DTO.Card;
using System.Threading.Tasks;

namespace Soriana.PPS.Card.AddCard.Services
{
    public interface IAddCardService
    {
        Task<AddCardResponse> AddCard(AddCardRequest entity, string SalesForceRequest);
        Task<AddCardResponse> UpdatePredeterminatedCard(AddCardRequest entity);
    }
}
