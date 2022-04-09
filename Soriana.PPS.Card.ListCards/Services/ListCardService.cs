using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Soriana.PPS.Common.DTO.Card;
using Soriana.PPS.DataAccess.Card;
using Soriana.PPS.Common.Constants;

namespace Soriana.PPS.Card.ListCards.Services
{
    public class ListCardService : IListCardsService
    {
        #region PrivateFields
        private readonly ILogger<ListCardService> _Logger;
        private readonly ICardContext _CardContext;
        #endregion

        #region Constructor
        public ListCardService(ILogger<ListCardService> logger,
                                ICardContext cardContext)
        {
            _Logger = logger;
            _CardContext = cardContext;
        }
        #endregion

        #region Public Methods
        public async Task<ListCardsResponse> GetCards(string ClientID)
        {
            #region Definiciones
            ListCardsResponse Response = new ListCardsResponse();
            List<CardsResponse> lstCards = new List<CardsResponse>();
            List<CardsResponse> lstCardsOmonel = new List<CardsResponse>();
            #endregion

            #region Tarjetas Omonel
            var listCardsOmonel = await _CardContext.GetCardsOmonel(ClientID);

            foreach(var cardOmonel in listCardsOmonel)
            {
                ClientDireccion clientDir = new ClientDireccion();
                CardsResponse omonel = new CardsResponse();

                omonel.paymentCardId = Guid.NewGuid().ToString();
                omonel.paymentCardToken = cardOmonel.ClientToken;
                omonel.paymentCardMask = cardOmonel.MaskCard;
                omonel.paymentCardBank = cardOmonel.PaymentMethod;
                omonel.paymentCardName = "";
                omonel.paymentCardBin = cardOmonel.BinCode.ToString();
                omonel.cvvRequired = cardOmonel.CvvRequired.ToString();
                omonel.nipRequired = cardOmonel.NipRequired.ToString();
                omonel.paymentCardIcon = CardIconConstants.CARD_ICON_OMONEL;
                omonel.isDefault = cardOmonel.IsDefault.ToString();

                #region Direccion
                var Direccion = await GetDetailTokenOmonel(cardOmonel.CardAccountNumber);

                foreach (var dir in Direccion)
                {
                    clientDir = new ClientDireccion();
                    clientDir.billToCity = dir.billToCity;
                    clientDir.billToCountry = dir.billToCountry;
                    clientDir.billToEmail = dir.billToEmail;
                    clientDir.billToFirstName = dir.billToFirstName;
                    clientDir.billToLastName = dir.billToLastName;
                    clientDir.billToPhoneNumber = dir.billToPhoneNumber;
                    clientDir.billToPostalCode = dir.billToPostalCode;
                    clientDir.billToState = dir.billToState;
                    clientDir.billToStreet1 = dir.billToStreet1;
                }

                omonel.AddressTC = clientDir;
                #endregion

                lstCardsOmonel.Add(omonel);
            }
            #endregion

            #region Tarjetas Bancarias
            var listCards = await _CardContext.GetCards(ClientID);

            foreach (var cardToken in listCards)
            {
                ClientDireccion clientDir = new ClientDireccion();
                CardsResponse card = new CardsResponse();

                card.paymentCardId = Guid.NewGuid().ToString(); 
                card.paymentCardToken = cardToken.ClientToken;
                card.paymentCardMask = cardToken.MaskCard;
                card.paymentCardBank = cardToken.PaymentMethod;
                card.paymentCardName = "";
                card.paymentCardBin = cardToken.BinCode.ToString();
                card.cvvRequired = cardToken.CvvRequired.ToString();
                card.nipRequired =  cardToken.NipRequired.ToString();             
                card.isDefault = cardToken.IsDefault.ToString();

                #region CardIcon Tarjetas Bancarias
                switch (cardToken.PaymentMethod)
                {
                    case "Amex":
                        card.paymentCardIcon = CardIconConstants.CARD_ICON_AMEX;
                        break;
                    case "Visa":
                        card.paymentCardIcon = CardIconConstants.CARD_ICON_VISA;
                        break;
                    case "Master Card":
                        card.paymentCardIcon = CardIconConstants.CARD_ICON_MASTERCARD;
                        break;
                    case "Omonel":
                        card.paymentCardIcon = CardIconConstants.CARD_ICON_OMONEL;
                        break;
                    default:
                        card.paymentCardIcon = CardIconConstants.CARD_ICON_LOYALTY;
                        break;
                }
                #endregion

                #region Direccion
                var Direccion = await GetDetailToken(cardToken.ClientToken);

                foreach(var dir in Direccion)
                {
                    clientDir = new ClientDireccion();
                    clientDir.billToCity = dir.billToCity;
                    clientDir.billToCountry = dir.billToCountry;
                    clientDir.billToEmail = dir.billToEmail;
                    clientDir.billToFirstName = dir.billToFirstName;
                    clientDir.billToLastName = dir.billToLastName;
                    clientDir.billToPhoneNumber = dir.billToPhoneNumber;
                    clientDir.billToPostalCode = dir.billToPostalCode;
                    clientDir.billToState = dir.billToState;
                    clientDir.billToStreet1 = dir.billToStreet1;                 
                }

                card.AddressTC = clientDir;
                #endregion

                lstCards.Add(card);
            }
            #endregion

            foreach(var omonel in lstCardsOmonel)
            {
                lstCards.Add(omonel);
            }

            Response.responseError = "0";
            Response.responseMessage = "";
            Response.paymentCards = lstCards;


            return Response;
        }
        #endregion

        #region Private Methods
        private async Task<IList<ClientAddressToken>> GetDetailToken(string ClientToken)
        {
            var ValidateTokenCard = await _CardContext.GetAddressClient(ClientToken);

            return ValidateTokenCard;
        }

        private async Task<IList<ClientAddressToken>> GetDetailTokenOmonel(string CardAccountNumber)
        {
            var ValidateTokenOmonel = await _CardContext.GetAddressClientOmonel(CardAccountNumber);

            return ValidateTokenOmonel;
        }

        #endregion
    }
}
