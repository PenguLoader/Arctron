import type { MessageBoxOptions, MessageBoxResult, OpenDialogOptions, SaveDialogOptions } from "../api/dialog";
import { getNativeBridge } from "./nativeBridge";

class PlatformDialog {
	openFile(options: OpenDialogOptions = {}): Promise<string[]> {
		return Promise.resolve(getNativeBridge().dialogOpenFile(options));
	}

	saveFile(options: SaveDialogOptions = {}): Promise<string | null> {
		return Promise.resolve(getNativeBridge().dialogSaveFile(options));
	}

	messageBox(options: MessageBoxOptions): Promise<MessageBoxResult> {
		return Promise.resolve(getNativeBridge().dialogMessageBox(options));
	}
}

export const dialog = new PlatformDialog();
