import type { TrayEvent, TrayMenuItem, TrayOptions } from "../api/tray";
import { EventEmitter } from "../internal/eventEmitter";
import { getNativeBridge } from "./nativeBridge";

const trays = new Map<number, Tray>();
let trayEventsInitialized = false;

function ensureTrayEventBridge(): void {
	if (trayEventsInitialized) {
		return;
	}

	getNativeBridge().trayOnEvent((trayId, event, menuItemId) => {
		const tray = trays.get(trayId);
		if (!tray) {
			return;
		}

		tray.emit(event, menuItemId);
	});

	trayEventsInitialized = true;
}

export class Tray {
	private id: number;
	private emitter = new EventEmitter();

	constructor(options: TrayOptions = {}) {
		const native = getNativeBridge();
		ensureTrayEventBridge();
		this.id = native.trayCreate(options);
		trays.set(this.id, this);

		if (options.tooltip) {
			native.traySetToolTip(this.id, options.tooltip);
		}

		if (options.menu) {
			native.traySetMenu(this.id, options.menu);
		}
	}

	setToolTip(text: string): void {
		getNativeBridge().traySetToolTip(this.id, text);
	}

	setContextMenu(menu: TrayMenuItem[]): void {
		getNativeBridge().traySetMenu(this.id, menu);
	}

	on(event: "click" | "double-click", handler: () => void): () => void;
	on(event: "menu-item-click", handler: (menuItemId: string) => void): () => void;
	on(event: TrayEvent, handler: (() => void) | ((menuItemId: string) => void)): () => void {
		return this.emitter.on(event, handler as (...args: unknown[]) => void);
	}

	emit(event: TrayEvent, menuItemId?: string): void {
		if (event === "menu-item-click") {
			if (!menuItemId) {
				return;
			}
			this.emitter.emit(event, menuItemId);
			return;
		}

		this.emitter.emit(event);
	}
}
