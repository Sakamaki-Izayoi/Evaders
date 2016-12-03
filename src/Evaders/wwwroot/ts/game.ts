/// <reference path="shared.ts" />
/// <reference path="communication.ts" />
/// <reference path="../typings/index.d.ts"/>


/* Varaibles injected by the backend */
declare var valid: boolean;
declare var url: string;

/**
 * Represents the textual representation of each game state
 */
class GameStates {
    static boot = "boot";
    static load = "load";
}


/**
 * Defines all objects used in the game 
 */
interface IObjectContainer {

    /*
     * BOOT / LOAD
     */

    logo: Phaser.Sprite;
    loadState: Phaser.Text;
}


/**
 * Base class for all game states.
 */
abstract class GameState {
    protected game: Phaser.Game;
    protected container: IObjectContainer;

    constructor(game: Phaser.Game, container: IObjectContainer) {
        this.game = game;
        this.container = container;
    }


    create(): void { }

    preload(): void { }

    render(): void { }

    update(): void { }
}

/**
 * Initial state which loads the basic systems
 */
class BootGameState extends GameState {
    create(): void {
        // set background color to match the logo
        this.game.stage.backgroundColor = "#1D0902";

        // add the logo
        this.container.logo = this.game.add.sprite(this.game.world.centerX, this.game.world.centerY, "logo");
        this.container.logo.anchor.setTo(0.5, 0.5);

        // add the text
        this.container.loadState = this.game.add.text(this.game.world.centerX,
            this.game.world.centerY + 65,
            "loading ...",
            { font: "13px Consolas", fill: "#FFFFFF", align: "center" });
        this.container.loadState.anchor.setTo(0.5, 0.5);

        // go to load state
        this.game.state.start(GameStates.load, false);
    }

    preload(): void {
        // load logo
        this.game.load.image("logo", "content/logo.png");
    }
}

/**
 * Game state which loads all required resources as well as the replay.
 */
class LoadGameState extends GameState {
    create(): void {
        // check the replay parameter
        const replayId = ParameterHelper.get("replay");
        if (!replayId) {
            this.container.loadState.text = "invalid configuration.";
            console.error("no replay id.");
            return;
        }

        // start the download
        $.ajax({
                dataType: "json",
                url: `/Replay/${replayId}`
            })
            .fail(() => {

            })
            .done((e) => {
               console.info(e); 
            });
    }
}


/**
 * Helper class which parses the url parameters and provides helper functions
 */
class ParameterHelper {
    static parameters: any = {};

    static load(): void {
        this.parameters = {}
        const items = decodeURIComponent(window.location.search.substr(1)).split("&");
        for (let i = 0; i < items.length; i++) {
            const data = items[i].split("=", 2);
            this.parameters[data[0]] = data[1];
        }
    }

    static get(name: string): string {
        const data = this.parameters[name];
        return data ? data : null;
    }

    static getDefault(name: string, def: any) {
        const data = this.parameters[name];
        return data ? data : def;
    }

    static has(name: string): boolean {
        if (this.parameters) return true;
        else return false;
    }
}


$(() => {

    // load url parameters
    ParameterHelper.load();

    // setup variables
    const game = new Phaser.Game(800, 600, Phaser.AUTO, "gameContainer");
    const container = {} as IObjectContainer;

    // register state
    game.state.add(GameStates.boot, new BootGameState(game, container));
    game.state.add(GameStates.load, new LoadGameState(game, container));

    // start booting
    game.state.start(GameStates.boot);
});