export type EventHandler<T extends unknown[] = unknown[]> = (...args: T) => void;

export class EventEmitter {
  private listeners = new Map<string, Set<EventHandler>>();

  on(event: string, handler: EventHandler): () => void {
    const set = this.listeners.get(event) ?? new Set<EventHandler>();
    set.add(handler);
    this.listeners.set(event, set);
    return () => this.off(event, handler);
  }

  once(event: string, handler: EventHandler): () => void {
    const wrapper: EventHandler = (...args) => {
      this.off(event, wrapper);
      handler(...args);
    };
    return this.on(event, wrapper);
  }

  off(event: string, handler: EventHandler): void {
    const set = this.listeners.get(event);
    if (!set) {
      return;
    }
    set.delete(handler);
    if (set.size === 0) {
      this.listeners.delete(event);
    }
  }

  emit(event: string, ...args: unknown[]): void {
    const set = this.listeners.get(event);
    if (!set) {
      return;
    }
    for (const handler of Array.from(set)) {
      handler(...args);
    }
  }
}
