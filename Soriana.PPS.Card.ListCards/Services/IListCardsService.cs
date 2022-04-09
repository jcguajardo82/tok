using System.Threading.Tasks;
using System.Collections.Generic;

using Soriana.PPS.Common.DTO.Card;

namespace Soriana.PPS.Card.ListCards.Services
{
    public interface IListCardsService
    {
        Task<ListCardsResponse> GetCards(string ClientID);
    }
}
