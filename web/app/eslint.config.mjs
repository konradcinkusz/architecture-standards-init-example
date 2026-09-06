// eslint-config-next 16 ships native flat configs, so there is no FlatCompat
// bridge here. Going through `@eslint/eslintrc` instead fails under ESLint 10
// with a circular-structure error out of the legacy config validator — the
// shareable config it is asked to translate is already flat.
import nextCoreWebVitals from "eslint-config-next/core-web-vitals";
import nextTypeScript from "eslint-config-next/typescript";

// Named rather than exported anonymously: `import/no-anonymous-default-export`
// warns on a bare array here, and this config lints itself.
const config = [
  ...nextCoreWebVitals,
  ...nextTypeScript,
  {
    ignores: [
      ".next/**",
      "node_modules/**",
      "next-env.d.ts",
    ],
  },
];

export default config;
