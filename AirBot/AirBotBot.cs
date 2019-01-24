using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace AirBot
{
    public class AirBotBot : IBot
    {
        private readonly AirBotAccessors _accessors;

        public AirBotBot(AirBotAccessors accessors)
        {
            _accessors = accessors;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var didWelcomeUser = await _accessors.WelcomeUserState.GetAsync(turnContext, () => new WelcomeUserState());

            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                await WelcomeNewUser(turnContext, didWelcomeUser, cancellationToken);

                var text = turnContext.Activity.Text.ToLowerInvariant();
                var responseText = ProcessInput(text);

                await turnContext.SendActivityAsync(responseText, cancellationToken: cancellationToken);
            }
            else if(turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if(turnContext.Activity.MembersAdded != null)
                {
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        if(member.Id != turnContext.Activity.Recipient.Id)
                        {
                            await turnContext.SendActivityAsync(Messages.WelcomeMessage, cancellationToken: cancellationToken);
                            await SendSuggestedActionsAsync(turnContext, cancellationToken);
                        }
                    }
                }
            }

            await _accessors.UserState.SaveChangesAsync(turnContext);
        }

        private static string ProcessInput(string text)
        {
            switch (text)
            {
                case "book a flight":
                    return "So you want to book a flight he?";
                case "get the weather forecast":
                    return "It's freezing outside!";
                default:
                    return "Sorry I didn't get that. Please pick one of the suggested options.";
            }
        }

        private async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply("Please pick your choice...");

            reply.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>
                {
                    new CardAction { Title = "Book a flight", Type = ActionTypes.ImBack, Value = "Book a flight" },
                    new CardAction { Title = "Get the weather forecast", Type = ActionTypes.ImBack, Value = "Get the weather forecast" },
                }
            };

            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private async Task WelcomeNewUser(ITurnContext turnContext, WelcomeUserState didWelcomeUser, CancellationToken cancellationToken)
        {
            if (didWelcomeUser.DidBotWelcomeUser == false)
            {
                didWelcomeUser.DidBotWelcomeUser = true;

                // Update user state flag to reflect bot handled first user interaction.

                await _accessors.WelcomeUserState.SetAsync(turnContext, didWelcomeUser);
                await _accessors.UserState.SaveChangesAsync(turnContext);

                var userName = turnContext.Activity.From.Name;

                await turnContext.SendActivityAsync(Messages.FirstWelcomingMessageToNewUser, cancellationToken: cancellationToken);
                await turnContext.SendActivityAsync(Messages.FirstWelcomingMessageToNewUserWhatCanIDo, cancellationToken: cancellationToken);
            }
        }
    }
}
