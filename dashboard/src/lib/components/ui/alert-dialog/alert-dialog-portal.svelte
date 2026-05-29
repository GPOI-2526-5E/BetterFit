<script lang="ts">
	import { Dialog as AlertDialogPrimitive } from 'bits-ui';

	let { ...restProps }: AlertDialogPrimitive.PortalProps = $props();
</script>

<AlertDialogPrimitive.Portal {...restProps} />
