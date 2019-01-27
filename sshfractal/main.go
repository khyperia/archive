package main

import (
	"fmt"
	"golang.org/x/crypto/ssh"
	"io/ioutil"
	"net"
	"bufio"
	"bytes"
	"io"
	"encoding/binary"
	"os"
	"math"
)

const (
	CSI = "\033["
	AnsiCursorTopLeft = CSI + "H"
	AnsiClearScreen = CSI + "2J"
	AspectRatio = 2.0
	MandelbrotIters = 246
)

type TerminalState struct {
	width  int
	height int
}

type FractalState struct {
	x    float64
	y    float64
	zoom float64
}

func (f FractalState) renderChar(xPix float64, yPix float64, terminalState TerminalState) int {
	aspectRatio := float64(AspectRatio) * float64(terminalState.height) / float64(terminalState.width)
	re := (xPix / float64(terminalState.width) * 2 - 1) * f.zoom + f.x
	im := (yPix / float64(terminalState.height) * 2 - 1) * aspectRatio * f.zoom + f.y
	re_c := re
	im_c := im
	for i := 0; i < MandelbrotIters; i++ {
		if re * re + im * im > 4 {
			return i
		}
		re_new := re * re - im * im
		im_new := re * im * 2
		re = re_new
		im = im_new
		re += re_c
		im += im_c
	}
	return 0
}

func valToStr(value float64) string {
	if value == 0 {
		return " "
	} else if value < 0.2 {
		return "'"
	} else if value < 0.4 {
		return "o"
	} else if value < 0.6 {
		return "*"
	} else if value < 0.8 {
		return "%"
	} else {
		return "@"
	}
}

func (f FractalState) render(buffer *bytes.Buffer, terminalState TerminalState) {
	for y := 0; y < terminalState.height; y++ {
		for x := 0; x < terminalState.width; x++ {
			antialias := 2
			val := 0
			for dx := 0; dx < antialias; dx++ {
				for dy := 0; dy < antialias; dy++ {
					xPix := float64(x * antialias + dx) / float64(antialias)
					yPix := float64(y * antialias + dy) / float64(antialias)
					val += f.renderChar(xPix, yPix, terminalState)
				}
			}
			value := float64(val) / float64(antialias * antialias)
			value = (1 - math.Cos(value / 8)) * 0.5
			buffer.WriteString(valToStr(value))
		}
		if y != terminalState.height - 1 {
			buffer.WriteString("\r\n")
		}
	}
}

func (f FractalState) redraw(channel ssh.Channel, terminalState TerminalState) {
	buffer := bytes.NewBuffer(make([]byte, 0, 100))
	buffer.WriteString(AnsiCursorTopLeft + AnsiClearScreen)
	f.render(buffer, terminalState)
	io.Copy(channel, buffer)
}

func client(channel ssh.Channel, terminalState *TerminalState) {
	reader := bufio.NewReader(channel)
	state := FractalState{0, 0, 1}
	moveSpeed := 0.1
	zoomSpeed := 1.1
	{
		buffer := bytes.NewBuffer(make([]byte, 0, 100))
		buffer.WriteString("Use WASD to move, RF to zoom, Q to quit\r\n")
		io.Copy(channel, buffer)
	}
	for {
		r, _, err := reader.ReadRune()
		if err != nil {
			fmt.Println(err)
			break
		}
		switch r {
		case 'w':
			state.y -= state.zoom * moveSpeed
		case 'a':
			state.x -= state.zoom * moveSpeed
		case 's':
			state.y += state.zoom * moveSpeed
		case 'd':
			state.x += state.zoom * moveSpeed
		case 'r':
			state.zoom /= zoomSpeed
		case 'f':
			state.zoom *= zoomSpeed
		case 'q', 27, 3:
			channel.CloseWrite()
			channel.Close()
			fmt.Println("User quit")
			return
		}
		state.redraw(channel, *terminalState)
	}
}

func discardAllButNormal(in <-chan *ssh.Request, state *TerminalState) {
	for req := range in {
		switch req.Type {
		case "pty-req":
			termlen := binary.BigEndian.Uint32(req.Payload[0:4])
			index := termlen + 4
			state.width = int(binary.BigEndian.Uint32(req.Payload[index:index + 4]))
			state.height = int(binary.BigEndian.Uint32(req.Payload[index + 4:index + 8]))
			req.Reply(true, nil)
			continue
		case "shell":
			req.Reply(true, nil)
			continue
		case "window-change":
			state.width = int(binary.BigEndian.Uint32(req.Payload[0:4]))
			state.height = int(binary.BigEndian.Uint32(req.Payload[4:8]))
			req.Reply(true, nil)
			continue
		}
		req.Reply(false, nil)
	}
}

func handleTcpCon(tcpCon net.Conn, config *ssh.ServerConfig) {
	server, chans, reqs, err := ssh.NewServerConn(tcpCon, config)
	if err != nil {
		fmt.Println("Failed to handshake: " + err.Error())
		return
	}
	fmt.Println("Connection from " + server.RemoteAddr().String())
	go ssh.DiscardRequests(reqs)
	for newChannel := range chans {
		if newChannel.ChannelType() != "session" {
			newChannel.Reject(ssh.UnknownChannelType, "unknown channel type")
			continue
		}
		channel, requests, err := newChannel.Accept()
		if err != nil {
			fmt.Println("Could not accept channel: " + err.Error())
			continue
		}
		terminalState := &TerminalState{}
		go discardAllButNormal(requests, terminalState)
		go client(channel, terminalState)
	}
}

func main() {
	config := &ssh.ServerConfig{
		NoClientAuth: true,
	}
	privateBytes, err := ioutil.ReadFile("id_rsa")
	if err != nil {
		panic("Failed to load id_rsa file: " + err.Error())
	}
	private, err := ssh.ParsePrivateKey(privateBytes)
	if err != nil {
		panic("Failed to parse id_rsa file: " + err.Error())
	}

	config.AddHostKey(private)

	listenAddr := "0.0.0.0:44444"
	if port, ok := os.LookupEnv("SSHFRACTAL_PORT"); ok {
		listenAddr = fmt.Sprintf("0.0.0.0:%s", port)
	}
	listener, err := net.Listen("tcp", listenAddr)
	if err != nil {
		panic("Failed to listen on " + listenAddr + ": " + err.Error())
	}

	fmt.Println("Listening on " + listenAddr)
	for {
		tcpCon, err := listener.Accept()
		if err != nil {
			panic("Failed to accept socket: " + err.Error())
		}
		go handleTcpCon(tcpCon, config)
	}
}
