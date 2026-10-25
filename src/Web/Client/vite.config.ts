import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/api/v1/agent": {
        target: "http://localhost:5003",
        changeOrigin: true,
        cookieDomainRewrite: "localhost",
      },
      "/api/v1": { target: "http://localhost:5000", changeOrigin: true },
      "/api/auth": { target: "http://localhost:5000", changeOrigin: true },
      "/api/tasks": { target: "http://localhost:5000", changeOrigin: true },
      "/api/documents": { target: "http://localhost:5000", changeOrigin: true },
      "/api/agent": {
        target: "http://localhost:5003",
        changeOrigin: true,
        cookieDomainRewrite: "localhost",
      },

      "/mcp": { target: "http://localhost:5000", changeOrigin: true },
    },
  },
});
