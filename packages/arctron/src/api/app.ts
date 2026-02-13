export type AppEvent = "ready" | "before-quit" | "window-all-closed" | "activate";

export interface App {
  whenReady(): Promise<void>;
  quit(): void;
  on(event: AppEvent, handler: () => void): () => void;
}
