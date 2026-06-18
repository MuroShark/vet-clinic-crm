<script lang="ts">
  import { app } from '$lib/stores/app.svelte';
  import Profile from '$lib/components/Profile.svelte';

  $effect(() => {
    app.activeView = 'profile';
  });
</script>

<Profile />
