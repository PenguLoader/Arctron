export interface PathApi {
  join(...parts: string[]): string;
  resolve(...parts: string[]): string;
  getUserData(): string;
  getLocalData(): string;
  getTemp(): string;
  getCwd(): string;
  getBaseExeDir(): string;
}
