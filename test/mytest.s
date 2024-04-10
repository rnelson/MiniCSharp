; mytest.cs
    .model small
    .586
    .stack 100h
    
; data segment
    .data
_S0 DB "Enter a number: ", '$'
_S1 DB "-b = ", '$'
_S2 DB "The answer to life, the universe, and everything is ", '$'
i dw ?
j dw ?
    
; code segment
    .code
    include io.asm
firstclass_firstclass2 proc
    push bp
    mov bp, sp
    push dx
    mov dx, OFFSET _S0
    call writestr
    pop dx
    push bx
    call readint
    mov [bp+8], bx
    pop bx
    mov ax, [bp+8]
    neg ax
    mov [bp-2], ax
    mov ax, [bp-2]
    mov [bp+8], ax
    push dx
    mov dx, OFFSET _S1
    call writestr
    pop dx
    mov ax, [bp+8]
    call writeint
    call writeln
    mov ax, 4
    mov i, ax
    mov ax, 2
    mov j, ax
    push dx
    mov dx, OFFSET _S2
    call writestr
    pop dx
    mov ax, i
    call writeint
    mov ax, j
    call writeint
    pop bp
    ret 4
firstclass_firstclass2 endp
someclass_Main proc
    push bp
    mov bp, sp
    sub sp, 30
    push [bp-4]
    push [bp-2]
    call firstclass_firstclass2
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