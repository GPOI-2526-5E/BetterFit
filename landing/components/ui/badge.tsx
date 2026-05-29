import type { ComponentProps } from "react";
import { cva, type VariantProps } from "class-variance-authority";

import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs font-semibold uppercase",
  {
    variants: {
      variant: {
        default:
          "border-primary/12 bg-primary/10 text-primary shadow-[0_10px_18px_rgba(23,105,255,0.08)]",
        energy:
          "border-[#d7ef8b] bg-[#f4fadf] text-[#486106] shadow-[0_10px_18px_rgba(184,242,29,0.12)]",
        outline: "border-border/80 bg-white/70 text-foreground",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
);

function Badge({
  className,
  variant,
  ...props
}: ComponentProps<"div"> & VariantProps<typeof badgeVariants>) {
  return (
    <div
      data-slot="badge"
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    />
  );
}

export { Badge, badgeVariants };
