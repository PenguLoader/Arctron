import { defineConfig } from "vite";
import arctron from "arctron/vite";

export default defineConfig({
  plugins: [arctron()],
  server: {
    port: 5173
  }
});
