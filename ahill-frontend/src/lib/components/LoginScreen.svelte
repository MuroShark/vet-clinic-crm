<script lang="ts">
  import { ShieldAlert, Key, UserCheck } from 'lucide-svelte';
  import { goto } from '$app/navigation';
  import { apiClient } from '$lib/api/client';
  import { app } from '$lib/stores/app.svelte';
  import { viewToPath } from '$lib/nav';

  let username = $state('');
  let password = $state('');
  let isLoading = $state(false);
  let errorMsg = $state<string | null>(null);

  async function handleSubmit(e: SubmitEvent) {
    e.preventDefault();
    isLoading = true;
    errorMsg = null;
    try {
      const response = await apiClient.auth.login(username, password);
      app.loginSuccess(response.user, response.employee);
      await goto(viewToPath(app.activeView));
    } catch (err: any) {
      errorMsg = err.message || 'Login failed. Please try again.';
    } finally {
      isLoading = false;
    }
  }

  function setCredentials(user: string) {
    username = user;
    password = 'demo123';
  }
</script>

<div class="min-h-screen bg-[#0D2E2B] flex flex-col items-center justify-center p-4 font-sans antialiased text-slate-300">
  <div class="bg-white border border-slate-200/80 w-full max-w-md p-8 rounded-[8px] shadow-xl space-y-6 text-slate-700">
    <div class="text-center space-y-3">
      <div class="mx-auto w-16 h-16 rounded-full bg-teal-700 flex items-center justify-center">
        <span class="font-bold text-white text-3xl select-none">V</span>
      </div>
      <div>
        <h1 class="text-xl font-bold tracking-tight text-slate-900">Veterinary Clinic</h1>
        <p class="text-[11px] uppercase tracking-wider text-teal-700 font-bold mt-1">Veterinary Information System</p>
      </div>
    </div>

    {#if errorMsg}
      <div class="p-3 bg-red-50 border border-red-200 rounded text-xs text-red-700 flex items-start gap-2">
        <ShieldAlert class="w-4 h-4 text-red-500 mt-0.5 shrink-0" />
        <span>{errorMsg}</span>
      </div>
    {/if}

    <form onsubmit={handleSubmit} class="space-y-4">
      <div class="space-y-1.5 text-xs">
        <label for="login-user" class="block text-slate-600 font-bold uppercase tracking-wider select-none">Login:</label>
        <div class="relative">
          <span class="absolute inset-y-0 left-0 pl-3 flex items-center text-slate-400 pointer-events-none">
            <UserCheck class="w-4 h-4" />
          </span>
          <input
            id="login-user"
            type="text"
            required
            placeholder="Enter your login"
            bind:value={username}
            class="w-full pl-9 pr-4 py-2.5 border border-slate-300 rounded-[6px] bg-slate-50 text-slate-900 placeholder-slate-400 text-xs focus:outline-none focus:border-teal-700 focus:bg-white focus:ring-1 focus:ring-teal-700/20 transition-colors"
          />
        </div>
      </div>

      <div class="space-y-1.5 text-xs">
        <label for="login-pass" class="block text-slate-600 font-bold uppercase tracking-wider select-none">Password:</label>
        <div class="relative">
          <span class="absolute inset-y-0 left-0 pl-3 flex items-center text-slate-400 pointer-events-none">
            <Key class="w-4 h-4" />
          </span>
          <input
            id="login-pass"
            type="password"
            required
            placeholder="Enter your password"
            bind:value={password}
            class="w-full pl-9 pr-4 py-2.5 border border-slate-300 rounded-[6px] bg-slate-50 text-slate-900 placeholder-slate-400 text-xs focus:outline-none focus:border-teal-700 focus:bg-white focus:ring-1 focus:ring-teal-700/20 transition-colors"
          />
        </div>
      </div>

      <button
        type="submit"
        disabled={isLoading}
        class="w-full bg-[#0F766E] hover:bg-teal-800 text-white py-2.5 rounded-[6px] text-xs font-semibold shadow transition-colors cursor-pointer disabled:opacity-50 mt-2"
      >
        {isLoading ? 'Authenticating...' : 'Sign In'}
      </button>
    </form>

    <div class="pt-5 border-t border-slate-100">
      <span class="text-[10px] uppercase font-bold text-slate-400 block mb-2.5 tracking-wider text-center">Demo Accounts (password: demo123):</span>
      <div class="grid grid-cols-2 gap-2 text-[11px]">
        <button onclick={() => setCredentials('l.hayes')} class="p-2 border border-slate-200 bg-slate-50 text-slate-700 rounded-[6px] hover:bg-teal-50 transition cursor-pointer text-left">
          • Receptionist: <span class="text-teal-700 font-semibold">l.hayes</span>
        </button>
        <button onclick={() => setCredentials('s.bennett')} class="p-2 border border-slate-200 bg-slate-50 text-slate-700 rounded-[6px] hover:bg-teal-50 transition cursor-pointer text-left">
          • Vet (Therapist): <span class="text-teal-700 font-semibold">s.bennett</span>
        </button>
        <button onclick={() => setCredentials('a.morgan')} class="p-2 border border-slate-200 bg-slate-50 text-slate-700 rounded-[6px] hover:bg-teal-50 transition cursor-pointer text-left">
          • Chief Vet: <span class="text-teal-700 font-semibold">a.morgan</span>
        </button>
        <button onclick={() => setCredentials('d.carter')} class="p-2 border border-slate-200 bg-slate-50 text-slate-700 rounded-[6px] hover:bg-teal-50 transition cursor-pointer text-left">
          • Director: <span class="text-teal-700 font-semibold">d.carter</span>
        </button>
      </div>
    </div>
  </div>
</div>
