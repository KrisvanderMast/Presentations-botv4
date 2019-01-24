using Microsoft.Bot.Builder;
using System;

namespace AirBot
{
    public class AirBotAccessors
    {
        public AirBotAccessors(UserState userState)
        {
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        }

        public static string WelcomeUserName { get; } = $"{nameof(AirBotAccessors)}.WelcomeUserState";

        public IStatePropertyAccessor<WelcomeUserState> WelcomeUserState { get; set; }

        public UserState UserState { get; }
    }

    public class WelcomeUserState
    {
        public bool DidBotWelcomeUser { get; set; } = false;
    }
}
