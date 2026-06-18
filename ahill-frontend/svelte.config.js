import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	compilerOptions: {
		// Принудительное включение режима runes для проекта, исключая библиотеки. Можно удалить в Svelte 6.
		runes: ({ filename }) => (filename.split(/[/\\]/).includes('node_modules') ? undefined : true)
	},
	kit: {
		// Чистое SPA (ssr=false по проекту): статическая сборка с SPA-fallback на index.html.
		paths: {
			base: '/vet-clinic-crm',
			relative: true
		},
		adapter: adapter({
			pages: 'build',
			assets: 'build',
			fallback: '404.html',
			precompress: false,
			strict: false
		})
	}
};

export default config;
