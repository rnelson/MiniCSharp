; hamer-mytest.cs
    .model small
    .586
    .stack 100h
    
; data segment
    .data
_S0 DB "The value of a is ", '$'

    
; code segment
    .code
    include io.asm
firstclass_firstclass proc
    push bp
    mov bp, sp
    pop bp
    ret
firstclass_firstclass endp
firstclass_secondclass proc
    push bp
    mov bp, sp
    sub sp, 12
    push bx
    mov ax, [bp-6]
    mov bx, [bp-8]
    imul bx
    mov [bp-10], ax
    pop bx
    mov ax, [bp-2]
    add ax, [bp-10]
    mov [bp-12], ax
    mov ax, [bp-12]
    mov [bp-4], ax
    mov ax, [bp-4]
    add sp, 12
    pop bp
    ret
firstclass_secondclass endp
someclass_Main proc
    push bp
    mov bp, sp
    sub sp, 30
    call firstclass_secondclass
    mov [bp-2], ax
    push dx
    mov dx, OFFSET _S0
    call writestr
    pop dx
    mov ax, [bp-2]
    call writeint
    call writeln
    add sp, 30
    pop bp
    ret
someclass_Main endp
start proc
    mov ax, @data
    mov ds, ax
    call someclass_Main
    mov al, 0
    mov ah, 4ch
    int 21h
start endp
      end start