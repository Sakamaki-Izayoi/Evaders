﻿namespace Evaders.Core.Networking
{
    using Newtonsoft.Json;

    public class Packet
    {
        public enum PacketTypeC2S
        {
            Authorize,
            GameAction,
            TurnEnd,
            EnterQueue,
            LeaveQueue,
            GetActiveGames,
            SwitchQueueMode
        }

        public enum PacketTypeS2C
        {
            AuthState,
            Kick,
            MoveTo,
            ShootProjectile,
            IllegalAction,
            NextRound,
            GameState,
            GameEnd,
            Message,
            QueueState
        }

        public PacketTypeS2C TypeS2C => (PacketTypeS2C) Type;
        public PacketTypeC2S TypeC2S => (PacketTypeC2S) Type;
        public readonly object Payload;
        public readonly int Type;

        public Packet(PacketTypeC2S type, object payload)
        {
            Type = (int) type;
            Payload = payload;
        }

        public Packet(PacketTypeS2C type, object payload)
        {
            Type = (int) type;
            Payload = payload;
        }

        public T GetPayload<T>()
        {
            if (Payload == null)
                return default(T);
            if (Payload is T)
                return (T) Payload;
            return JsonConvert.DeserializeObject<T>(Payload.ToString());
        }
    }
}