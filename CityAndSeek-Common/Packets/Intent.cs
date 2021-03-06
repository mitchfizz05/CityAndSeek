﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CityAndSeek.Common.Packets
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Intent
    {
        /// <summary>
        /// Informing the other party that an error occured.
        /// </summary>
        Error,

        /// <summary>
        /// <b>Client -> Server</b><br />
        /// Create a new game.<br />
        /// Payload is a Game object with the requested values set.
        /// </summary>
        CreateGame,

        /// <summary>
        /// <b>Server -> Client</b><br />
        /// Sent in response to <i>CreateGame</i>.<br />
        /// Payload is a Game object.
        /// </summary>
        GameCreated,

        /// <summary>
        /// <b>Client -> Server</b><br />
        /// Sent when a client wants to join a game.
        /// </summary>
        JoinGame,

        /// <summary>
        /// <b>Server -> Client</b><br />
        /// Sent in response to <i>JoinGame</i>.<br />
        /// Payload is a WelcomePayload.
        /// </summary>
        Welcome,

        /// <summary>
        /// <b>Server -> Client</b><br />
        /// Sends the entire game state (the full game object) to the client.
        /// </summary>
        GameState,

        /// <summary>
        /// <b>Client -> Server</b><br />
        /// Sends the new position of the player. This should be sent every few seconds or so.
        /// </summary>
        PositionUpdate,

        /// <summary>
        /// <b>Server -> Client</b><br />
        /// Sends the new public position of player(s) to the client.<br />
        /// This will be sent whenever the public position of player(s) changes.
        /// </summary>
        ServerPositionUpdate,
    }
}
