/// <reference path="shared.ts" />
/// <reference path="communication.ts" />
/// <reference path="../typed/phazer/phazer.d.ts" />
/// <reference path="../typed/jquery/jquery.d.ts" />
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
/**
 * Represents the textual representation of each game state
 */
var GameStates = (function () {
    function GameStates() {
    }
    return GameStates;
}());
GameStates.boot = "boot";
GameStates.load = "load";
/**
 * Base class for all game states.
 */
var GameState = (function () {
    function GameState(game, container) {
        this.game = game;
        this.container = container;
    }
    GameState.prototype.create = function () {
    };
    GameState.prototype.preload = function () {
    };
    GameState.prototype.render = function () {
    };
    GameState.prototype.update = function () {
    };
    return GameState;
}());
/**
 * Initial state which loads the basic systems
 */
var BootGameState = (function (_super) {
    __extends(BootGameState, _super);
    function BootGameState() {
        return _super.apply(this, arguments) || this;
    }
    BootGameState.prototype.create = function () {
        // set background color to match the logo
        this.game.stage.backgroundColor = "#1D0902";
        // add the logo
        this.container.logo = this.game.add.sprite(this.game.world.centerX, this.game.world.centerY, "logo");
        this.container.logo.anchor.setTo(0.5, 0.5);
        // add the text
        this.container.loadState = this.game.add.text(this.game.world.centerX, this.game.world.centerY + 65, "loading ...", { font: "13px Consolas", fill: "#FFFFFF", align: "center" });
        this.container.loadState.anchor.setTo(0.5, 0.5);
        // go to load state
        this.game.state.start(GameStates.load, false);
    };
    BootGameState.prototype.preload = function () {
        // load logo
        this.game.load.image("logo", "content/logo.png");
    };
    return BootGameState;
}(GameState));
/**
 * Game state which loads all required resources as well as the replay.
 */
var LoadGameState = (function (_super) {
    __extends(LoadGameState, _super);
    function LoadGameState() {
        return _super.apply(this, arguments) || this;
    }
    LoadGameState.prototype.create = function () {
        // check the replay parameter
        var replayId = ParameterHelper.get("replay");
        if (!replayId) {
            this.container.loadState.text = "invalid configuration.";
            console.error("no replay id.");
            return;
        }
        // start the download
        $.ajax({
            dataType: "json",
            url: "/Replay/" + replayId
        })
            .fail(function () {
        })
            .done(function (e) {
            console.info(e);
        });
    };
    return LoadGameState;
}(GameState));
/**
 * Helper class which parses the url parameters and provides helper functions
 */
var ParameterHelper = (function () {
    function ParameterHelper() {
    }
    ParameterHelper.load = function () {
        this.parameters = {};
        var items = decodeURIComponent(window.location.search.substr(1)).split("&");
        for (var i = 0; i < items.length; i++) {
            var data = items[i].split("=", 2);
            this.parameters[data[0]] = data[1];
        }
    };
    ParameterHelper.get = function (name) {
        var data = this.parameters[name];
        return data ? data : null;
    };
    ParameterHelper.getDefault = function (name, def) {
        var data = this.parameters[name];
        return data ? data : def;
    };
    ParameterHelper.has = function (name) {
        if (this.parameters)
            return true;
        else
            return false;
    };
    return ParameterHelper;
}());
ParameterHelper.parameters = {};
$(function () {
    // load url parameters
    ParameterHelper.load();
    // setup variables
    var game = new Phaser.Game(800, 600, Phaser.AUTO, "gameContainer");
    var container = {};
    // register state
    game.state.add(GameStates.boot, new BootGameState(game, container));
    game.state.add(GameStates.load, new LoadGameState(game, container));
    // start booting
    game.state.start(GameStates.boot);
});
