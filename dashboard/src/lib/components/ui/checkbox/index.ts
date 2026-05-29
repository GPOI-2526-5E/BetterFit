import { Checkbox as CheckboxPrimitive } from 'bits-ui';

import Root from './checkbox.svelte';

const Group = CheckboxPrimitive.Group;
const GroupLabel = CheckboxPrimitive.GroupLabel;

export {
	Root,
	Group,
	GroupLabel,
	Root as Checkbox,
	Group as CheckboxGroup,
	GroupLabel as CheckboxGroupLabel
};
