using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace AirBot
{
    public class AirBotBot : IBot
    {
        private readonly AirBotAccessors _accessors;
        private readonly DialogSet _dialogs;

        public AirBotBot(AirBotAccessors accessors)
        {
            _accessors = accessors;

            _dialogs = new DialogSet(accessors.ConversationDialogState);

            var waterfallSteps = new WaterfallStep[]
            {
                FromStepAsync,
                ToStepAsync,
                HowManyPeopleStepAsync,
                SummaryStepAsync
            };

            _dialogs.Add(new WaterfallDialog("booking", waterfallSteps));
            _dialogs.Add(new TextPrompt("from"));
            _dialogs.Add(new TextPrompt("to"));
            _dialogs.Add(new NumberPrompt<int>("howmany"));
            _dialogs.Add(new ConfirmPrompt("confirm"));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var didWelcomeUser = await _accessors.WelcomeUserState.GetAsync(turnContext, () => new WelcomeUserState());

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                await WelcomeNewUser(turnContext, didWelcomeUser, cancellationToken);

                var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync("booking", null, cancellationToken);
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded != null)
                {
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            await turnContext.SendActivityAsync(Messages.WelcomeMessage, cancellationToken: cancellationToken);
                            await SendSuggestedActionsAsync(turnContext, cancellationToken);
                        }
                    }
                }
            }

            await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private static string ProcessInput(string text, ITurnContext turnContext)
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

        private static Attachment CreateAdaptiveCardAttachment()
        {
            var adaptiveCardJson = File.ReadAllText(@".\Resources\flight.json");
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
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
        private async Task<DialogTurnResult> FromStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync("from", new PromptOptions { Prompt = MessageFactory.Text("Where do you want to start your journey?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> ToStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Update the profile.
            userProfile.From = (string)stepContext.Result;

            return await stepContext.PromptAsync("to", new PromptOptions { Prompt = MessageFactory.Text("Where do you want to go to?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> HowManyPeopleStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Update the profile.
            userProfile.To = (string)stepContext.Result;

            return await stepContext.PromptAsync("howmany", new PromptOptions { Prompt = MessageFactory.Text("How many people?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
            userProfile.HowMany = (int)stepContext.Result;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I booked your flight {userProfile.From}, going to {userProfile.To} for {userProfile.HowMany}."), cancellationToken);
            await stepContext.Context.SendActivityAsync("Have a great time over there!", cancellationToken: cancellationToken);

            var reply = stepContext.Context.Activity.CreateReply();
            reply.Attachments = new List<Attachment>
            {
                CreateAdaptiveCardAttachment()
            };

            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            await SendSuggestedActionsAsync(stepContext.Context, cancellationToken);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
