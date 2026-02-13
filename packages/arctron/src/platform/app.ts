import type { App } from "../api/app";
import { EventEmitter } from "../internal/eventEmitter";
import { getNativeBridge } from "./nativeBridge";

class PlatformApp implements App {
	private emitter = new EventEmitter();
	private readyPromise: Promise<void>;

	constructor() {
		this.readyPromise = Promise.resolve().then(() => {
			this.emitter.emit("ready");
		});
	}

	whenReady(): Promise<void> {
		return this.readyPromise;
	}

	quit(): void {
		getNativeBridge().appQuit();
	}

	on(event: "ready" | "before-quit" | "window-all-closed" | "activate", handler: () => void): () => void {
		return this.emitter.on(event, handler);
	}
}

export const app: App = new PlatformApp();
