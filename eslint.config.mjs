import tseslint from "typescript-eslint";

export default tseslint.config(
    {
        ignores: ["src/HighLow/wwwroot/js/**", "src/HighLow/wwwroot/lib/**"],
    },
    ...tseslint.configs.recommended,
    {
        files: ["src/HighLow/wwwroot/ts/**/*.ts"],
        rules: {
            "@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_" }],
        },
    },
);
