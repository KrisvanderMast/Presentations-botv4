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

        public IStatePropertyAccessor<bool> DidWelcomeUser { get; set; }

        public UserState UserState { get; }
    }

    public class WelcomeUserState
    {
        public bool DidAirBotSayHello { get; set; } = false;
    }

    public class Messages
    {
        internal const string WelcomeMessage = @"Hello, I'm AirBot. How may I help you?";
        internal const string WhatCanIDoMessage = @"You can ask me ...";
    }
}
