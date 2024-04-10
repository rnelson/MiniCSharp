	.model small
	.stack 100h
	.data
_S0	db	"The value of a is ","$"
	.code
	include io.asm
firstclass	proc
firstclass	endp

secondclass	proc
		push	bp
		mov	bp,sp
		sub	sp,12

		; put some real values in
		mov ax, 2
		mov [bp-2], ax
		mov ax, 3
		mov [bp-6], ax
		mov ax, 4
		mov [bp-8], ax

		mov	ax,[bp-6]
		mov	bx,[bp-8]
		imul	bx
		mov	[bp-10],ax

		mov	ax,[bp-2]
		add	ax,[bp-10]
		mov	[bp-12],ax

		mov	ax,[bp-12]
		mov	[bp-4],ax

		mov	ax,[bp-4]

		add	sp,12
		pop	bp
		ret	
secondclass	endp

Main		proc
		push	bp
		mov	bp,sp
		sub	sp,2

		call secondclass

		mov	[bp-2], ax
		mov 	dx, OFFSET _S0
		call 	writestr

		mov 	ax, [bp-2]
		call	writeint

		call	writeln

		add	sp,2
		pop	bp
		ret	

Main		endp

start		proc
		mov ax,@data
		mov ds,ax
		
		call Main

		mov ah,4ch
		int 21h
start		endp
		end start