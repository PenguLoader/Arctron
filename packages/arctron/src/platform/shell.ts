import { getNativeBridge } from "./nativeBridge";

class PlatformShell {
	openURL(url: string): Promise<void> {
		getNativeBridge().shellOpenExternal(url);
		return Promise.resolve();
	}

	revealPath(path: string): Promise<void> {
		getNativeBridge().shellRevealPath(path);
		return Promise.resolve();
	}

	trashPath(path: string): Promise<void> {
		getNativeBridge().shellTrashPath(path);
		return Promise.resolve();
	}

	openExternal(url: string): Promise<void> {
		return this.openURL(url);
	}

	openPath(path: string): Promise<void> {
		getNativeBridge().shellOpenPath(path);
		return Promise.resolve();
	}
}

export const shell = new PlatformShell();
