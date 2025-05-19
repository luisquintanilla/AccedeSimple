import { startTransition } from 'react';
import { Message, AssistantMessage } from '../types/ChatTypes';
import { UnboundedChannel } from '../utils/UnboundedChannel';

class ChatService {
    private static instance: ChatService;
    private backendUrl: string;
    private activeStream?: { eventSource: EventSource, channel: UnboundedChannel<Message> };

    private constructor(backendUrl: string) {
        this.backendUrl = backendUrl;
    }

    static getInstance(backendUrl: string): ChatService {
        if (!ChatService.instance) {
            ChatService.instance = new ChatService(backendUrl);
        }
        return ChatService.instance;
    }

    async *stream(
        startIndex: number,
        abortController: AbortController
    ): AsyncGenerator<Message> {
        const abortHandler = () => {
            console.log('Aborting chat stream');
            if (this.activeStream) {
                this.activeStream.eventSource.close();
                this.activeStream.channel.close();
                this.activeStream = undefined;
            }
        };

        abortController.signal.addEventListener('abort', abortHandler);

        let index = startIndex || 0;
        try {
            while (!abortController.signal.aborted) {
                let channel = new UnboundedChannel<Message>();

                try {
                    const eventSource = new EventSource(`${this.backendUrl}/chat/stream?startIndex=${index}`);

                    // Store the event source and channel
                    this.activeStream = { eventSource, channel };

                    // Handle messages
                    eventSource.addEventListener('message', (event) => {
                        try {
                            // Parse the raw message data
                            const rawMessage = JSON.parse(event.data);
                            console.log('Received message:', rawMessage);

                            // 'assistant' messages have an isFinal property which indicates if the message is final or not.
                            // Non-final messages are used for streaming responses.
                            let isFinal = true;
                            if (rawMessage.role === 'assistant') {
                                const assistantMsg = rawMessage as AssistantMessage;
                                isFinal = assistantMsg.isFinal !== undefined ? assistantMsg.isFinal : true;
                            }

                            if (isFinal) {
                                index++;
                            }

                            // Pass through the message preserving all fields from the server
                            channel.write(rawMessage);
                        } catch (error) {
                            console.error(`Error processing SSE message: ${error}`);
                        }
                    });

                    // Handle complete event
                    eventSource.addEventListener('complete', () => {
                        console.debug('Stream completed');
                        eventSource.close();
                        channel.close();
                    });

                    // Handle error event
                    eventSource.addEventListener('error', event => {
                        console.error(`Stream error: ${event}`);
                        eventSource.close();
                        channel.throwError(new Error('SSE connection failed'));
                    });

                    try {
                        for await (const message of channel) {
                            yield message;
                        }
                    } finally {
                        eventSource.close();
                    }
                } catch (error) {
                    console.error('Stream error:', error);
                    if (abortController.signal.aborted) {
                        break;
                    }
                } finally {
                    this.activeStream = undefined;
                }

                if (!abortController.signal.aborted) {
                    console.debug('Retrying stream in 1 second');
                    await new Promise(resolve => setTimeout(resolve, 1000));
                }
            }
        } finally {
            abortController.signal.removeEventListener('abort', abortHandler);
        }
    }

    async sendMessage(text: string, files?: File[]): Promise<void> {
        files = files || [];
        const formData = new FormData();
        formData.append('Text', text);

        // Append each file to the form data
        files.forEach(file => {
            formData.append('file', file);
        });

        const response = await fetch(`${this.backendUrl}/chat/messages`, {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            let errorMessage;
            try {
                errorMessage = await response.text();
            } catch (e) {
                errorMessage = response.statusText;
            }
            throw new Error(`Error sending message with attachments: ${errorMessage}`);
        }
    }

    async cancelChat(): Promise<void> {
        const response = await fetch(`${this.backendUrl}/chat/stream/cancel`, {
            method: 'POST'
        });
        if (!response.ok) {
            throw new Error('Failed to cancel chat');
        }
    }

    
    async getChat(): Promise<Message[]> {
        console.log('Backend URL:', this.backendUrl);
        console.log('Request URL:', `${this.backendUrl}/chat/messages`);
        const response = await fetch(`${this.backendUrl}/chat/messages?userId=terry-carnation`, {
            method: 'GET',
        });
        if (!response.ok) {
            throw new Error('Failed to fetch chat history');
        }
        return await response.json();
    }

    async selectItinerary(messageId: string, optionId: string): Promise<void> {
        const response = await fetch(`${this.backendUrl}/chat/select-itinerary`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ messageId, optionId })
        });

        if (!response.ok) {
            let errorMessage;
            try {
                errorMessage = await response.text();
            } catch (e) {
                errorMessage = response.statusText;
            }
            throw new Error(`Error selecting itinerary: ${errorMessage}`);
        }
    }

    async clearMessages(userId: string): Promise<void> {
        const response = await fetch(`${this.backendUrl}/chat/messages/clear?userId=${userId}`, {
            method: 'POST',
        });

        if (!response.ok) {
            throw new Error('Failed to clear messages');
        }
    }

}
export default ChatService;
