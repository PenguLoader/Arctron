import type { BrowserWindowOptions } from "../api/browserWindow";
import { getNativeBridge } from "./nativeBridge";

type RpcHandler = (...args: unknown[]) => Promise<unknown> | unknown;

const windowRpcHandlers = new Map<number, Map<string, RpcHandler>>();

const arctronGlobal = globalThis as typeof globalThis & {
	__arctronOnWindowRpcInvoke?: (windowId: number, requestId: string, method: string, argsJson: string) => void;
};

if (!arctronGlobal.__arctronOnWindowRpcInvoke) {
	arctronGlobal.__arctronOnWindowRpcInvoke = (windowId: number, requestId: string, method: string, argsJson: string): void => {
		const native = getNativeBridge();
		const handlers = windowRpcHandlers.get(windowId);
		const handler = handlers?.get(method);

		if (!handler) {
			native.windowRpcReject(windowId, requestId, `RPC method not found: ${method}`);
			return;
		}

		let args: unknown[];
		try {
			const parsed = JSON.parse(argsJson);
			args = Array.isArray(parsed) ? parsed : [parsed];
		} catch {
			native.windowRpcReject(windowId, requestId, "Invalid RPC arguments payload");
			return;
		}

		try {
			const result = handler(...args);
			if (result && typeof (result as { then?: unknown }).then === "function") {
				(result as Promise<unknown>)
					.then((value) => {
						native.windowRpcResolve(windowId, requestId, JSON.stringify(value ?? null));
					})
					.catch((error) => {
						native.windowRpcReject(windowId, requestId, error instanceof Error ? error.message : String(error));
					});
				return;
			}

			native.windowRpcResolve(windowId, requestId, JSON.stringify(result ?? null));
		} catch (error) {
			native.windowRpcReject(windowId, requestId, error instanceof Error ? error.message : String(error));
		}
	};
}

export class BrowserWindow {
	private id: number;

	constructor(options: BrowserWindowOptions = {}) {
		const native = getNativeBridge();
		const methodEntries = Object.entries(options.rpc?.methods ?? {});
		const methodNames = methodEntries.map(([name]) => name);
		const rpcNamespace = options.rpc?.namespace ?? null;
		const nativeOptions = {
			...options,
			rpc: undefined,
			rpcNamespace,
			rpcMethods: methodNames,
		};

		this.id = native.windowCreate(nativeOptions);

		if (options.title) {
			native.windowSetTitle(this.id, options.title);
		}

		if (methodEntries.length > 0) {
			windowRpcHandlers.set(this.id, new Map(methodEntries));
			native.windowSetRpcManifest(this.id, rpcNamespace, methodNames);
		}

		if (options.initialUrl) {
			native.windowLoadUrl(this.id, options.initialUrl);
		}

		if (options.show !== false) {
			native.windowShow(this.id);
		}
	}

	loadURL(url: string): void {
		getNativeBridge().windowLoadUrl(this.id, url);
	}

	show(): void {
		getNativeBridge().windowShow(this.id);
	}

	hide(): void {
		getNativeBridge().windowHide(this.id);
	}

	close(): void {
		windowRpcHandlers.delete(this.id);
		getNativeBridge().windowClose(this.id);
	}

	setTitle(title: string): void {
		getNativeBridge().windowSetTitle(this.id, title);
	}

	getSize(): [number, number] {
		const [width, height] = getNativeBridge().windowGetSize(this.id);
		return [width ?? 0, height ?? 0];
	}

	getPosition(): [number, number] {
		const [x, y] = getNativeBridge().windowGetPosition(this.id);
		return [x ?? 0, y ?? 0];
	}

	focus(): void {
		getNativeBridge().windowFocus(this.id);
	}

	center(): void {
		getNativeBridge().windowCenter(this.id);
	}

	minimize(): void {
		getNativeBridge().windowMinimize(this.id);
	}

	unminimize(): void {
		getNativeBridge().windowUnminimize(this.id);
	}

	isMinimized(): boolean {
		return getNativeBridge().windowIsMinimized(this.id);
	}

	maximize(): void {
		getNativeBridge().windowMaximize(this.id);
	}

	unmaximize(): void {
		getNativeBridge().windowUnmaximize(this.id);
	}

	isMaximized(): boolean {
		return getNativeBridge().windowIsMaximized(this.id);
	}
}
