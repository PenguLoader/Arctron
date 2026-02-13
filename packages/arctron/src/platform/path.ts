import type { PathApi } from "../api/path";
import { getNativeBridge } from "./nativeBridge";

class PlatformPath implements PathApi {
  join(...parts: string[]): string {
    return getNativeBridge().pathJoin(parts);
  }

  resolve(...parts: string[]): string {
    return getNativeBridge().pathResolve(parts);
  }

  getUserData(): string {
    return getNativeBridge().pathGetUserData();
  }

  getLocalData(): string {
    return getNativeBridge().pathGetLocalData();
  }

  getTemp(): string {
    return getNativeBridge().pathGetTemp();
  }

  getCwd(): string {
    return getNativeBridge().pathGetCwd();
  }

  getBaseExeDir(): string {
    return getNativeBridge().pathGetBaseExeDir();
  }
}

export const path = new PlatformPath();
