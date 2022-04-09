using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Soriana.PPS.Common.DTO.Card;
using Soriana.PPS.DataAccess.Card;

namespace Soriana.PPS.Card.RemoveCard.Services
{
    public class RemoveCardService : IRemoveCardService
    {
        #region PrivateFields
        private readonly ILogger<RemoveCardService> _Logger;
        private readonly ICardContext _CardContext;
        #endregion

        #region Constuctor
        public RemoveCardService(ILogger<RemoveCardService> logger,
                                 ICardContext cardContext)
        {
            _Logger = logger;
            _CardContext = cardContext;
        }
        #endregion

        #region Public Methods
        public async Task<RemoveCardResponse> RemoveCard(RemoveCardRequest removeCard)
        {         
            RemoveCardResponse Response = new RemoveCardResponse();

            string CardToken = removeCard.paymentCardToken.Substring(0, 6);

            if (CardToken != "OMONEL")
            {
                var responseRemove = await _CardContext.RemoveCard(removeCard);

                Response.responseError = "0";
                Response.responseMessage = "";

                if (responseRemove[0].IsActive == false)
                {
                    Response.responseError = "0";
                    Response.responseMessage = "";
                }
                else
                {
                    Response.responseError = "15";
                    Response.responseMessage = "Error al eliminar Tarjeta";
                }
            }              
            else if (CardToken == "OMONEL")
            {
                var responseRemove = await _CardContext.RemoveCardOmonel(removeCard);

                if(responseRemove[0].IsActive == false)
                {
                    Response.responseError = "0";
                    Response.responseMessage = "";
                }
                else
                {
                    Response.responseError = "15";
                    Response.responseMessage = "Error al eliminar Tarjeta";
                }

                
            }               
            else
            {
                Response.responseError = "99";
                Response.responseMessage = "Tipo de Tarjeta Incorrecta";
            }
            
            

            return Response;
        }
        #endregion
    }
}
