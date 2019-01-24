using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;

namespace AirBot
{
    public class AirBotAccessors
    {
        public AirBotAccessors(ConversationState conversationState, UserState userState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        }

        public static string WelcomeUserName { get; } = $"{nameof(AirBotAccessors)}.WelcomeUserState";

        public IStatePropertyAccessor<WelcomeUserState> WelcomeUserState { get; set; }
        public IStatePropertyAccessor<DialogState> ConversationDialogState { get; set; }
        public ConversationState ConversationState { get; }
        public UserState UserState { get; }
        public IStatePropertyAccessor<UserProfile> UserProfile { get;  set; }
    }

    public class WelcomeUserState
    {
        public bool DidBotWelcomeUser { get; set; } = false;
    }
}
