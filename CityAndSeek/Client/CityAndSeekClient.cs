﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Maps.Model;
using Android.Gms.Tasks;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using CityAndSeek.Client.Events;
using CityAndSeek.Client.RequestHandlers;
using CityAndSeek.Common;
using CityAndSeek.Common.Packets;
using CityAndSeek.Common.Packets.Payloads;
using CityAndSeek.Helpers;
using Newtonsoft.Json;
using WebSocketSharp;
using Intent = CityAndSeek.Common.Packets.Intent;
using LatLng = CityAndSeek.Common.LatLng;
using Task = System.Threading.Tasks.Task;

namespace CityAndSeek.Client
{
    public class CityAndSeekClient
    {
        public event EventHandler<GameStateUpdateEvent> OnGameStateUpdate; 

        public string Url { get; private set; }
        public WebSocket WebSocket { get; protected set; }

        /// <summary>
        /// Current player instance.
        /// </summary>
        public Player Player { get; set; }

        /// <summary>
        /// Get a unique request ID
        /// </summary>
        public int RequestId => _requestId++;

        protected List<IRequestHandler> RequestHandlers;

        /// <summary>
        /// Links packet Ids to Actions which are supposed to handle them.
        /// </summary>
        private IDictionary<int, TaskCompletionSource<Packet>> _pendingActions = new Dictionary<int, TaskCompletionSource<Packet>>();
        /// <summary>
        /// Each time a unique request ID is needed, this is used and incremented.
        /// </summary>
        private int _requestId = 0;

        public CityAndSeekClient(string url)
        {
            Url = url;

            OnGameStateUpdate += OnGameStateUpdateHandler;

            var gameStateUpdateHandler = new GameStateUpdateHandler(this);
            gameStateUpdateHandler.OnGameStateUpdate += (sender, e) => OnGameStateUpdate?.Invoke(sender, e);

            RequestHandlers = new List<IRequestHandler>
            {
                gameStateUpdateHandler
            };
        }

        protected Task<Packet> AwaitResponse(int requestId)
        {
            _pendingActions.Add(requestId, new TaskCompletionSource<Packet>());

            return _pendingActions[requestId].Task;
        }

        public async Task<bool> Connect()
        {
            WebSocket = new WebSocket(Url);
            WebSocket.ConnectAsync();

            WebSocket.OnMessage += OnWebSocketMessage;

            TaskCompletionSource<bool> result = new TaskCompletionSource<bool>();

            EventHandler success = null;
            EventHandler<ErrorEventArgs> error = null;
            DelayedAction Timeout = null;

            Action unregisterActions = () =>
            {
                WebSocket.OnOpen -= success;
                WebSocket.OnError -= error;
                Timeout.Abort();
            };

            WebSocket.OnOpen += success = (sender, args) =>
            {
                unregisterActions();

                result.SetResult(true);
            };
            WebSocket.OnError += error = (sender, args) =>
            {
                unregisterActions();

                result.SetResult(false);
            };
            Timeout = new DelayedAction(() =>
            {
                unregisterActions();

                result.SetResult(false);
            });
            Timeout.Run(3000); // 3 second timeout

            await result.Task;
            return result.Task.Result;
        }

        protected void OnWebSocketMessage(object sender, MessageEventArgs e)
        {
            Packet packet = JsonConvert.DeserializeObject<Packet>(e.Data);

            if (_pendingActions.ContainsKey(packet.Id))
            {
                // We have been awaiting this packet
                _pendingActions[packet.Id].SetResult(packet);
                _pendingActions.Remove(packet.Id);
            }

            // Dispatch packet to request handlers
            foreach (IRequestHandler handler in RequestHandlers)
                handler.OnPacket(packet);
        }

        /// <summary>
        /// Create a new City & Seek game.
        /// </summary>
        /// <param name="game">Requested game options. Not everything has to be set.</param>
        /// <returns>Created game</returns>
        public async Task<Common.Game> CreateGameAsync(Common.Game game)
        {
            int requestId = RequestId;
            WebSocket.SendAsync(JsonConvert.SerializeObject(new Packet(Intent.CreateGame, game, requestId)), b => { }); // todo: handle error here
            
            
            var resp = await AwaitResponse(requestId);
            return resp.GetPayload<Common.Game>();
        }

        /// <summary>
        /// Join an existing City & Seek game.
        /// </summary>
        /// <param name="gameId">Game ID</param>
        /// <param name="gamePassword">Game password</param>
        /// <param name="username">Username to use</param>
        /// <returns>Welcome payload</returns>
        public async Task<WelcomePayload> JoinGameAsync(int gameId, string gamePassword, string username)
        {
            var joinGamePayload = new JoinGamePayload(gameId, gamePassword, username);

            int requestId = RequestId;
            WebSocket.SendAsync(JsonConvert.SerializeObject(new Packet(Intent.JoinGame, joinGamePayload, requestId)), b => { }); // todo: handle errors
            
            var resp = await AwaitResponse(requestId);
            return resp.GetPayload<WelcomePayload>();
        }

        /// <summary>
        /// Send new position to the server.
        /// </summary>
        /// <param name="newLocation">New position</param>
        /// <returns>Success?</returns>
        public async Task<bool> SendPositionUpdateAsync(LatLng newLocation)
        {
            var payload = new PositionUpdatePayload(newLocation);

            TaskCompletionSource<bool> success = new TaskCompletionSource<bool>();

            WebSocket.SendAsync(JsonConvert.SerializeObject(new Packet(Intent.PositionUpdate, payload)), b =>
            {
                success.SetResult(b);
            });

            return await success.Task;
        }

        private void OnGameStateUpdateHandler(object sender, GameStateUpdateEvent e)
        {
            Log.Debug(CityAndSeekApp.Tag, "Received new game state.");
            Player.Game = e.Game;
        }
    }
}