<script lang="ts">
	import { Dialog as AlertDialogPrimitive } from 'bits-ui';

	let { open = $bindable(false), ...restProps }: AlertDialogPrimitive.RootProps = $props();
</script>

<AlertDialogPrimitive.Root bind:open {...restProps} />
