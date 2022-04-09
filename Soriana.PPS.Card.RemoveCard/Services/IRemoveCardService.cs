using System.Threading.Tasks;

using Soriana.PPS.Common.DTO.Card;

namespace Soriana.PPS.Card.RemoveCard.Services
{
    public interface IRemoveCardService
    {
        Task<RemoveCardResponse> RemoveCard(RemoveCardRequest removeCard);
    }
}
