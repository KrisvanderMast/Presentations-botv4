﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

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
                //FromConfirmStepAsync,
                ToStepAsync,
                //ToConfirmStepAsync,
                HowManyPeopleStepAsync,
                //HowManyPeopleConfirmStepAsync,
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

            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                await WelcomeNewUser(turnContext, didWelcomeUser, cancellationToken);

                var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync("booking", null, cancellationToken);
                }

                //var text = turnContext.Activity.Text.ToLowerInvariant();
                //var responseText = ProcessInput(text, turnContext);

                //await turnContext.SendActivityAsync(responseText, cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> FromConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Update the profile.
            userProfile.From = (string)stepContext.Result;

            // We can send messages to the user at any point in the WaterfallStep.
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thanks {stepContext.Result}."), cancellationToken);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync("confirm", new PromptOptions { Prompt = MessageFactory.Text("Would you like to give your age?") }, cancellationToken);
        }
        private async Task<DialogTurnResult> ToStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Update the profile.
            userProfile.From = (string)stepContext.Result;

            return await stepContext.PromptAsync("to", new PromptOptions { Prompt = MessageFactory.Text("Where do you want to go to?") }, cancellationToken);
        }

        private Task<DialogTurnResult> ToConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<DialogTurnResult> HowManyPeopleStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Update the profile.
            userProfile.To = (string)stepContext.Result;

            return await stepContext.PromptAsync("howmany", new PromptOptions { Prompt = MessageFactory.Text("How many people?") }, cancellationToken);
        }
        private Task<DialogTurnResult> HowManyPeopleConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Update the profile.
            userProfile.HowMany = (int)stepContext.Result;

            //if ((bool)stepContext.Result)
            //{
                //var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I booked your flight {userProfile.From}, going to {userProfile.To} for {userProfile.HowMany}."), cancellationToken);
                await stepContext.Context.SendActivityAsync("Have a great time over there!", cancellationToken: cancellationToken);
            //}

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
