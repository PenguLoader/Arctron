import fs from "node:fs";
import path from "node:path";
import { spawn, type ChildProcess } from "node:child_process";
import type { Plugin, ResolvedConfig } from "vite";
import { loadConfig } from "../config/loadConfig";

export type ArctronOptions = {
    enabled?: boolean;
    hostFilter?: string;
    hostScript?: string;
    workspaceRoot?: string;
    mainDevScript?: string;
    mainReadyTimeoutMs?: number;
};

function findWorkspaceRoot(startDir: string): string {
    let current = path.resolve(startDir);

    while (true) {
        if (fs.existsSync(path.join(current, "pnpm-workspace.yaml"))) {
            return current;
        }

        const parent = path.dirname(current);
        if (parent === current) {
            return path.resolve(startDir);
        }

        current = parent;
    }
}

function arctron(options: ArctronOptions = {}): Plugin {
    const name = "arctron-vite-plugin";
    let resolvedConfig: ResolvedConfig | undefined;
    let mainProcessBuilder: ChildProcess | undefined;
    let hostProcess: ChildProcess | undefined;
    let shutdownBound = false;

    const enabled = options.enabled ?? true;
    const hostFilter = options.hostFilter ?? "@arctron/host";
    const hostScript = options.hostScript ?? "dev";
    const mainDevScript = options.mainDevScript ?? "dev:main";
    const mainReadyTimeoutMs = options.mainReadyTimeoutMs ?? 15_000;
    const packageRunner = process.platform === "win32" ? "pnpm.cmd" : "pnpm";

    const wait = (ms: number) => new Promise<void>((resolve) => setTimeout(resolve, ms));

    const waitForFile = async (filePath: string, timeoutMs: number) => {
        const start = Date.now();
        while (!fs.existsSync(filePath)) {
            if (Date.now() - start >= timeoutMs) {
                return false;
            }

            await wait(150);
        }

        return true;
    };

    const pipePrefixedLogs = (child: ChildProcess, tag: string) => {
        child.stdout?.on("data", (chunk) => {
            const message = chunk.toString().trimEnd();
            if (message) {
                resolvedConfig?.logger.info(`[${tag}] ${message}`);
            }
        });

        child.stderr?.on("data", (chunk) => {
            const message = chunk.toString().trimEnd();
            if (message) {
                resolvedConfig?.logger.error(`[${tag}] ${message}`);
            }
        });
    };

    const ensureMainProcessBuilt = async (appRoot: string, mainOutputAbsolute: string) => {
        if (mainProcessBuilder) {
            return;
        }

        const watcher = spawn(
            packageRunner,
            ["run", mainDevScript],
            {
                cwd: appRoot,
                env: process.env,
                stdio: ["ignore", "pipe", "pipe"]
            }
        );

        mainProcessBuilder = watcher;
        pipePrefixedLogs(watcher, "arctron-main");
        watcher.on("close", (code) => {
            resolvedConfig?.logger.info(`[${name}] main watcher exited with code ${code ?? 0}`);
            if (mainProcessBuilder === watcher) {
                mainProcessBuilder = undefined;
            }
        });

        const isReady = await waitForFile(mainOutputAbsolute, mainReadyTimeoutMs);
        if (!isReady) {
            throw new Error(
                `[${name}] timed out waiting for main output: ${mainOutputAbsolute}. Ensure script '${mainDevScript}' builds arctron.config.main.`
            );
        }
    };

    const stopHost = () => {
        if (mainProcessBuilder && !mainProcessBuilder.killed) {
            mainProcessBuilder.kill("SIGTERM");
        }

        mainProcessBuilder = undefined;

        if (!hostProcess) {
            return;
        }

        if (!hostProcess.killed) {
            hostProcess.kill("SIGTERM");
        }

        hostProcess = undefined;
    };

    const bindProcessShutdown = () => {
        if (shutdownBound) {
            return;
        }

        shutdownBound = true;
        process.once("exit", stopHost);
        process.once("SIGINT", stopHost);
        process.once("SIGTERM", stopHost);
    };

    return {
        name,
        enforce: "pre",

        configResolved(config) {
            resolvedConfig = config;
        },

        async configureServer(server) {
            if (!enabled || !resolvedConfig) {
                return;
            }

            const appRoot = resolvedConfig.root;
            const workspaceRoot = options.workspaceRoot ?? findWorkspaceRoot(appRoot);
            const arctronConfig = loadConfig(appRoot);
            const mainEntryAbsolute = path.resolve(appRoot, arctronConfig.main);

            if (hostProcess) {
                return;
            }

            bindProcessShutdown();

            await ensureMainProcessBuilt(appRoot, mainEntryAbsolute);
            resolvedConfig.logger.info(`[${name}] starting Arctron host`);

            const child = spawn(
                packageRunner,
                ["--filter", hostFilter, hostScript],
                {
                    cwd: workspaceRoot,
                    env: {
                        ...process.env,
                        ARCTRON_MAIN: mainEntryAbsolute
                    },
                    stdio: ["ignore", "pipe", "pipe"]
                }
            );

            hostProcess = child;
            pipePrefixedLogs(child, "arctron-host");

            child.on("close", (code) => {
                resolvedConfig?.logger.info(`[${name}] host exited with code ${code ?? 0}`);
                hostProcess = undefined;
            });

            server.httpServer?.once("close", stopHost);
        }
    };
}

export default arctron;