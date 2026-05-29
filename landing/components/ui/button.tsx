import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Slot } from "radix-ui"

import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "group/button inline-flex shrink-0 items-center justify-center gap-2 rounded-[10px] border border-transparent text-sm font-semibold whitespace-nowrap transition-all duration-150 outline-none select-none focus-visible:ring-[3px] focus-visible:ring-ring/30 focus-visible:ring-offset-0 active:not-aria-[haspopup]:translate-y-px disabled:pointer-events-none disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-[3px] aria-invalid:ring-destructive/20 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default:
          "bg-primary [background-image:linear-gradient(180deg,rgba(255,255,255,0.08),rgba(255,255,255,0)_42%),linear-gradient(135deg,#1769ff_0%,#1460eb_100%)] text-primary-foreground shadow-[0_10px_22px_rgba(23,105,255,0.18)] hover:-translate-y-0.5 hover:shadow-[0_14px_28px_rgba(23,105,255,0.2)]",
        outline:
          "border-border/80 bg-background/90 text-foreground shadow-[0_1px_2px_rgba(12,20,36,0.04)] hover:border-primary/25 hover:bg-secondary/80 hover:text-primary aria-expanded:bg-secondary/80 aria-expanded:text-primary dark:border-input dark:bg-input/30 dark:hover:bg-input/50",
        secondary:
          "bg-secondary text-secondary-foreground shadow-[0_1px_2px_rgba(12,20,36,0.04)] hover:bg-primary/10 hover:text-primary aria-expanded:bg-primary/10 aria-expanded:text-primary",
        ghost: "text-foreground hover:bg-secondary/80 hover:text-primary aria-expanded:bg-secondary/80 aria-expanded:text-primary dark:hover:bg-muted/50",
        destructive:
          "bg-destructive text-white shadow-[0_12px_26px_rgba(220,38,38,0.18)] hover:-translate-y-[1px] hover:bg-[#b91c1c] dark:focus-visible:ring-destructive/40",
        link: "text-primary underline-offset-4 hover:underline",
      },
      size: {
        default:
          "h-10 px-4 py-2 has-data-[icon=inline-end]:pr-3 has-data-[icon=inline-start]:pl-3",
        xs: "h-7 rounded-[10px] px-2.5 text-xs",
        sm: "h-9 rounded-[10px] px-3 text-[0.8rem]",
        lg: "h-11 rounded-[10px] px-6 text-sm has-data-[icon=inline-end]:pr-4 has-data-[icon=inline-start]:pl-4",
        icon: "size-10",
        "icon-xs":
          "size-7 rounded-[10px] [&_svg:not([class*='size-'])]:size-3",
        "icon-sm":
          "size-8 rounded-[10px]",
        "icon-lg": "size-11 rounded-[10px]",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "default",
  size = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }) {
  const Comp = asChild ? Slot.Root : "button"

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Button, buttonVariants }
