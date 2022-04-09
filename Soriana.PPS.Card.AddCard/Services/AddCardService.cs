using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

using Soriana.PPS.Common.Constants;
using Soriana.PPS.Common.DTO.Card;
using Soriana.PPS.DataAccess.Card;
using System;

namespace Soriana.PPS.Card.AddCard.Services
{
    public class AddCardService : IAddCardService
    {
        #region PrivateFields
        private readonly ILogger<AddCardService> _Logger;
        private readonly ICardContext _CardContext;
        #endregion

        #region Constructor
        public AddCardService(ILogger<AddCardService> logger,
                              ICardContext cardContext)
        {
            _Logger = logger; 
            _CardContext = cardContext;
        }
        #endregion

        #region Public Methods
        public async Task<AddCardResponse> AddCard(AddCardRequest entity, string SalesForceRequest)
        {
            string paymentCardId = string.Empty;
            AddCardResponse CardResponse = new AddCardResponse();
            CardTokenRequest Card = new CardTokenRequest();
            string OmonelToken = string.Empty;
            
            #region Save Json Request
            string strSalesForceRequest = SalesForceRequest.Replace("\"", "");
            await _CardContext.SaveRequestSalesForce(entity.customerId, strSalesForceRequest);
            #endregion
          
            var BinCard = await _CardContext.GetBinCard(entity.binCard);

            #region BinCode
            if (BinCard.Count != 0)
            {
                if (BinCard[0].Id_Cve_Bin == ConfigurationConstants.OMONEL_BIN_CODE)
                {
                    var guid = Guid.NewGuid();
                    Card.CardAccountNumber = entity.cardAccountNumber;
                    Card.NipRequired = true;
                    Card.CvvRequired = false;
                    Card.ClientToken = "OMONEL-" + guid.ToString();
                    OmonelToken = "OMONEL-" + guid.ToString();
                }
                else
                {
                    #region PaymentCard
                    int iniToken = entity.paymentCardId.IndexOf("-") + 1;
                    int Len = entity.paymentCardId.Length;
                    paymentCardId = entity.paymentCardId.Substring(iniToken, Len - iniToken);
                    #endregion

                    Card.NipRequired = false;
                    Card.CvvRequired = true;
                    Card.ClientToken = entity.paymentCardId;
                }

                Card.Bank = BinCard[0].Nom_EmisorTjta;              
                Card.BinCode = int.Parse(BinCard[0].Id_Cve_Bin);
                Card.ClientID = entity.customerId;
                Card.CustomerID = entity.customerId;              
                Card.paymentCardId = paymentCardId;
                Card.PaymentType = BinCard[0].Desc_Marca;
                Card.TypeOfCard = BinCard[0].Desc_TipoTjta;
                Card.PersistToken = bool.Parse(entity.setAsdefault);
                Card.IsActive = true;
                Card.MaskCard = entity.binCard.Substring(0, 4) + "-XXXX-XXXX-" + entity.suffix;               
                Card.IsDefault = bool.Parse(entity.setAsdefault);              
            }
            else
            {
                CardResponse.responseError = "12";
                CardResponse.responseMessage = "BinCode no se encuentra registrado";
                CardResponse.paymentCardMask = "";
                CardResponse.paymentCardName = "";
                CardResponse.paymentCardToken = "";
                CardResponse.paymentCardBank = "";
                CardResponse.paymentCardIcon = "";

                return CardResponse;
            }
            #endregion

            if (entity.cardAccountNumber != "")
            {
                if (BinCard[0].Id_Cve_Bin == ConfigurationConstants.OMONEL_BIN_CODE)
                {
                    CardResponse = await CardsToken(Card, entity, OmonelToken);
                }
                else
                {
                    #region Programa Lealtad
                    #region Json Request
                    LoyaltyAccountRequest LoyaltyRequest = new LoyaltyAccountRequest
                    {
                        Action = "2",
                        ClientId = entity.customerId,
                        Nombre = "", // entity.billToFirstName,
                        Paterno = "",  //entity.billToLastName,
                        Materno = "",
                        EMail = "" //entity.billToEmail
                    };
                    string JsonLoyaltyAccountRequest = JsonConvert.SerializeObject(LoyaltyRequest);
                    #endregion

                    #region HTTP 
                    string MBSWebAPI = "https://loyaltyaccount.azurewebsites.net/api/LoyaltyAccount";

                    FmkTools.RestResponse responseApi = FmkTools.RestClient.RequestRest_1(FmkTools.HttpVerb.POST, MBSWebAPI, null, JsonLoyaltyAccountRequest);
                    string jsonResponse = responseApi.message;

                    LoyaltyAccountResponse LoyaltyResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<LoyaltyAccountResponse>(jsonResponse);

                    CardResponse.responseError = LoyaltyResponse.Cve_RespCode;
                    CardResponse.responseMessage = LoyaltyResponse.Desc_MensajeError;
                    CardResponse.paymentCardMask = LoyaltyResponse.maskCard;
                    CardResponse.paymentCardName = ""; //entity.billToFirstName + " " + entity.billToLastName;
                    CardResponse.paymentCardToken = entity.paymentCardId;
                    CardResponse.paymentCardBank = entity.cardType;
                    CardResponse.paymentCardIcon = "";
                    #endregion
                    #endregion
                }

                if (CardResponse.responseError == "0")
                {
                    await CustomerInfo(entity);
                }
            }
            else
            {
                CardResponse = await CardsToken(Card, entity, OmonelToken);

                if(CardResponse.responseError == "0")
                {
                    await CustomerInfo(entity);
                }

            }          
           

            return CardResponse;
        }

        public async Task<AddCardResponse> UpdatePredeterminatedCard(AddCardRequest entity)
        {
            AddCardResponse CardResponse = new AddCardResponse();

            if (entity.binCard == ConfigurationConstants.OMONEL_BIN_CODE)
            {
                var ValidateOmonelCard = await _CardContext.GetClientDetailOmonel_APP(entity.customerId, entity.paymentCardId);
                var DetailOmonelCard = await _CardContext.GetClientDetailOmonel(entity.customerId, ValidateOmonelCard[0].CardAccountNumber);

                if (ValidateOmonelCard.Count == 0)
                {
                    CardResponse.responseError = "09";
                    CardResponse.responseMessage = "Tarjeta Omonel no se encuentra previamente registrada";
                    CardResponse.paymentCardMask = "";
                    CardResponse.paymentCardName = "";
                    CardResponse.paymentCardToken = "";
                    CardResponse.paymentCardBank = "";
                    CardResponse.paymentCardIcon = "";
                }
                else
                {
                    await _CardContext.UpdatePredeterminatedCardOmonel_APP(entity.paymentCardId, entity.customerId);

                    CardResponse.responseError = "00";
                    CardResponse.responseMessage = "";
                    CardResponse.paymentCardMask = ValidateOmonelCard[0].MaskCard;
                    CardResponse.paymentCardName = DetailOmonelCard[0].billToFirstName + " " + DetailOmonelCard[0].billToLastName;
                    CardResponse.paymentCardToken = ValidateOmonelCard[0].CardAccountNumber;
                    CardResponse.paymentCardBank = ValidateOmonelCard[0].Bank;
                    CardResponse.paymentCardIcon = "";
                }
            }
            else
            {
                var ValidateTokenCard = await _CardContext.GetClientDetail(entity.customerId, entity.paymentCardId);

                if (ValidateTokenCard.Count == 0)
                {
                    CardResponse.responseError = "09";
                    CardResponse.responseMessage = "Tarjeta no se encuentra previamente registrada";
                    CardResponse.paymentCardMask = "";
                    CardResponse.paymentCardName = "";
                    CardResponse.paymentCardToken = "";
                    CardResponse.paymentCardBank = "";
                    CardResponse.paymentCardIcon = "";
                }
                else
                {
                    await _CardContext.UpdatePredeterminatedCard(entity.paymentCardId, entity.customerId);

                    CardResponse.responseError = "00";
                    CardResponse.responseMessage = "";
                    CardResponse.paymentCardMask = ValidateTokenCard[0].MaskCard;
                    CardResponse.paymentCardName = ValidateTokenCard[0].billToFirstName + " " + ValidateTokenCard[0].billToLastName; ;
                    CardResponse.paymentCardToken = ValidateTokenCard[0].ClientToken;
                    CardResponse.paymentCardBank = ValidateTokenCard[0].Bank;
                    CardResponse.paymentCardIcon = "";
                }
            }

            return CardResponse;
        }
        #endregion

        #region Private Methods
        public async Task<AddCardResponse> CardsToken(CardTokenRequest Card, AddCardRequest entity, string OmonelToken)
        {
            AddCardResponse CardResponse = new AddCardResponse();
            IList<ValidateTokenResponse> ValidateTokenCard = null;

            if (Card.BinCode == int.Parse(ConfigurationConstants.OMONEL_BIN_CODE))
            {
                ValidateTokenCard = await _CardContext.ValidateOmonelCard(Card);
            }              
            else
            {
                ValidateTokenCard = await _CardContext.ValidateTokenCard(Card);
            }
                

            if (ValidateTokenCard.Count == 0)
            {
                if (Card.BinCode == int.Parse(ConfigurationConstants.OMONEL_BIN_CODE))
                     await _CardContext.SaveCardOmonel(Card);               
                else
                    await _CardContext.SaveCard(Card);

                CardResponse.responseError = "0";
                CardResponse.responseMessage = "";
                CardResponse.paymentCardMask = entity.binCard.Substring(0, 4) + "-XXXX-XXXX-" + entity.suffix;
                CardResponse.paymentCardName = "";
                CardResponse.paymentCardToken = entity.paymentCardId;
                CardResponse.paymentCardIcon = "";

                if (Card.BinCode == int.Parse(ConfigurationConstants.OMONEL_BIN_CODE))
                {
                    CardResponse.paymentCardBank = "OMONEL";
                    CardResponse.paymentCardToken = OmonelToken;
                }
                else
                {
                    CardResponse.paymentCardBank = Card.Bank;
                    CardResponse.paymentCardToken = entity.paymentCardId;
                }
                

            }
            else if(ValidateTokenCard[0].IsActive == false)
            {
                if (Card.BinCode == int.Parse(ConfigurationConstants.OMONEL_BIN_CODE))
                    await _CardContext.UpdateCardOmonel(entity.customerId, entity.cardAccountNumber);
                else
                    await _CardContext.UpdateCard(entity.customerId, entity.paymentCardId);

                CardResponse.responseError = "0";
                CardResponse.responseMessage = "";
                CardResponse.paymentCardMask = entity.binCard.Substring(0, 4) + "-XXXX-XXXX-" + entity.suffix;
                CardResponse.paymentCardName = "";
                CardResponse.paymentCardIcon = "";

                if (Card.BinCode == int.Parse(ConfigurationConstants.OMONEL_BIN_CODE))
                {
                    CardResponse.paymentCardBank = "OMONEL";
                    CardResponse.paymentCardToken = OmonelToken;
                }
                else
                {
                    CardResponse.paymentCardBank = Card.Bank;
                    CardResponse.paymentCardToken = entity.paymentCardId;
                }                                 
            }
            else
            {
                CardResponse.responseError = "06";
                CardResponse.responseMessage = "Tarjeta ya se encuentra previamente registrada";
                CardResponse.paymentCardMask = "";
                CardResponse.paymentCardName = "";
                CardResponse.paymentCardToken = "";
                CardResponse.paymentCardBank = "";
                CardResponse.paymentCardIcon = "";
            }

            return CardResponse;
        }

        public async Task CustomerInfo(AddCardRequest entity)
        {
            ClientCard client = new ClientCard
            {
                ClientID = entity.customerId,
                billToEmail = entity.billToEmail,
                billToFirstName = entity.billToFirstName,
                billToLastName = entity.billToLastName,
                billToCity = entity.billToCity,
                billToCountry = entity.billToCountry,
                billToPhoneNumber = entity.billToPhoneNumber,
                billToPostalCode = entity.billToPostalCode,
                billToState = entity.billToState,
                billToStreet1 = entity.billToStreet1
            };

            if (entity.binCard == ConfigurationConstants.OMONEL_BIN_CODE)
                client.ClientToken = entity.cardAccountNumber;
            else
                client.ClientToken = entity.paymentCardId;

            await _CardContext.SaveCustomerInfo(client);
        }
        #endregion
    }
}
