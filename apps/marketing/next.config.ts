import type { NextConfig } from "next";

// When deploying to GitHub Pages the base path is the repo name.
// Set NEXT_PUBLIC_BASE_PATH=/orchest-flow-ai in CI; leave unset for local dev.
const basePath = process.env.NEXT_PUBLIC_BASE_PATH ?? '';

const nextConfig: NextConfig = {
  output: "export",
  trailingSlash: true,
  transpilePackages: ['@orchest-flow-ai/web-public'],
  basePath,
  assetPrefix: basePath || undefined,
};

export default nextConfig;
