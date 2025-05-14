import React, { useState, useEffect, useRef, useCallback } from 'react';
import ChatService from '../services/ChatService';
import { Message, UserMessage, AssistantMessage, FileAttachment } from '../types/ChatTypes';
import ChatContainer from './ChatContainer';
import VirtualizedChatList from './VirtualizedChatList';
import logo from '../logo.svg';
import '../styles/index.css';

const loadingIndicatorId = 'loading-indicator';

const App: React.FC = () => {
    const [messages, setMessages] = useState<Message[]>([]);
    const [prompt, setPrompt] = useState<string>('');
    const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const abortControllerRef = useRef<AbortController | null>(null);
    const [shouldAutoScroll, setShouldAutoScroll] = useState<boolean>(true);
    const [streamingMessageId, setStreamingMessageId] = useState<string | null>(null);

    const chatService = ChatService.getInstance('/api');

    // Fetch chat history on load
    // useEffect(() => {
    //     (async () => {
    //         try {
    //             const chatHistory = await chatService.getChat();
    //             setMessages(chatHistory as Message[]);
    //         } catch (error) {
    //             console.error('Error fetching chat history:', error);
    //         }
    //     })();
    // }, [chatService]);

    useEffect(() => {
        if (abortControllerRef.current) {
            abortControllerRef.current.abort();
        }
        const abortController = new AbortController();
        abortControllerRef.current = abortController;

        (async () => {
            try {
                console.log('Starting chat stream');
                const stream = chatService.stream(0, abortController);
                
                let currentResponseId: string | null = null;
                
                for await (const message of stream) {
                    // Skip messages without a type
                    if (!message.type) continue;
                    
                    // Check if this is a new streaming response or continuation
                    if (message.id && message.id !== currentResponseId) {
                        currentResponseId = message.id;
                        if (message.type === 'assistant') {
                            const assistantMsg = message as AssistantMessage;
                            if (!assistantMsg.isFinal) {
                                setStreamingMessageId(currentResponseId);
                            }
                        }
                    }
                    
                    // For assistant messages, check if they're final
                    if (message.type === 'assistant') {
                        const assistantMsg = message as AssistantMessage;
                        if (assistantMsg.isFinal) {
                            setStreamingMessageId(null);
                            currentResponseId = null;
                            if (!assistantMsg.text) continue;
                        }
                    }

                    // Process message
                    processMessage(message);
                }
            } catch (error) {
                console.error('Stream error:', error);
            } finally {
                console.log('Chat stream finished');
                setStreamingMessageId(null);
            }
        })();

        return () => {
            abortController.abort();
        };
    }, [chatService]);

    // Process messages from the stream
    const processMessage = (message: Message) => {
        // If it's an assistant message, check isFinal
        if (message.type === 'assistant') {
            const assistantMsg = message as AssistantMessage;
            updateMessageById(
                message.id,
                message.text,
                message.role,
                message.type,
                assistantMsg.isFinal || false,
                message.attachments,
                message
            );
        }
        // For all other message types (including candidate-itineraries)
        else {
            updateMessageById(
                message.id,
                message.text,
                message.role,
                message.type,
                true, // Non-streaming messages are always final
                message.attachments,
                message // Pass the complete message to preserve all fields
            );
        }
    };

    const updateMessageById = (
        id: string, 
        newText: string, 
        role: string, 
        type: string, 
        isFinal: boolean = false, 
        attachments: FileAttachment[] | undefined = undefined,
        originalMessage?: Message // New parameter to store the complete original message
    ) => {
        const generatingReplyMessageText = 'Generating reply...';

        function getMessageText(existingText: string | undefined, newText: string): string {
            if (!existingText) {
                return newText;
            }
            
            if (existingText === generatingReplyMessageText) {
                return newText;
            }

            if (existingText.startsWith(generatingReplyMessageText)) {
                return existingText.replace(generatingReplyMessageText, '') + newText;
            }

            if (isFinal) {
                return newText; // For final messages, replace the entire text
            }

            if (newText.startsWith(existingText)) {
                return newText;
            }

            return existingText + newText;
        }

        setMessages(prevMessages => {
            const lastUserMessage = prevMessages.filter(m => m.role === 'user').slice(-1)[0];
            if (isFinal && lastUserMessage && lastUserMessage.text === newText && lastUserMessage.role === role) {
                return prevMessages;
            }
            
            const existingMessage = prevMessages.find(msg => msg.id === id);
            
            if (existingMessage) {
                return prevMessages.map(msg =>
                    msg.id === id 
                        ? {
                            // Start with the complete original message if available
                            ...(originalMessage || msg),
                            // Update specific fields
                            text: getMessageText(msg.text, newText),
                            role: role || msg.role,
                            type: type || msg.type,
                            attachments: attachments || msg.attachments,
                            // For assistant messages, update isFinal
                            ...(type === 'assistant' ? { isFinal } : {})
                        }
                        : msg
                );
            } else {
                return [...prevMessages.filter(msg => msg.id !== loadingIndicatorId),
                // If we have the original message, use it as the base; otherwise create one with provided fields
                originalMessage ? 
                    { ...originalMessage, id } : 
                    { 
                        id, 
                        role, 
                        text: newText, 
                        type: type, 
                        ...(type === 'assistant' ? { isFinal } : {}),
                        attachments
                    }
                ];
            }
        });
    };

    useEffect(() => {
        // Instead of direct scroll listener, we'll rely on the checkScroll function in ChatContainer
        if (shouldAutoScroll && messagesEndRef.current) {
            // Using scrollTo instead of scrollIntoView for better control
            messagesEndRef.current.scrollTo({
                top: messagesEndRef.current.scrollHeight,
                behavior: streamingMessageId ? 'smooth' : 'auto'
            });
        }
    }, [messages, shouldAutoScroll, streamingMessageId]);

    const handleSubmit = useCallback(async (e: React.FormEvent) => {
        e.preventDefault();
        if ((!prompt.trim() && selectedFiles.length === 0) || streamingMessageId) return;

        // Create a message object that includes file attachments if any
        const userMessage: UserMessage = {
            id: `user-${Date.now()}`, 
            role: 'user', 
            text: prompt, 
            type: 'user',
            attachments: selectedFiles.length > 0 ? selectedFiles.map(file => ({
                uri: URL.createObjectURL(file),
                contentType: file.type
            })) : undefined
        };

        setMessages(prevMessages => [...prevMessages, userMessage]);

        setMessages(prevMessages => [
            ...prevMessages,
            { id: loadingIndicatorId, role: 'assistant', text: 'Generating reply...', type: 'assistant' }
        ]);

        try {
            await chatService.sendMessage(prompt, selectedFiles);
            setPrompt('');
            setSelectedFiles([]); // Clear selected files after sending
        } catch (error) {
            console.error('handleSubmit error:', error);
            setMessages(prev =>
                prev.map(msg =>
                    msg.id === loadingIndicatorId ? { ...msg, text: '[Error in receiving response]' } : msg
                )
            );
        }
    }, [prompt, selectedFiles, streamingMessageId, chatService]);

    const cancelChat = () => {
        if (!streamingMessageId) return;
        chatService.cancelChat();
    };

    // Function to handle itinerary selection
    const selectItinerary = async (messageId: string, optionId: string) => {
        try {
            // Show loading message
            setMessages(prevMessages => [
                ...prevMessages,
                { id: loadingIndicatorId, role: 'assistant', text: 'Processing your selection...', type: 'assistant' }
            ]);

            // Send the selection to the API
            await chatService.selectItinerary(messageId, optionId);
        } catch (error) {
            console.error('Error selecting itinerary:', error);

            // Show error message
            setMessages(prev =>
                prev.map(msg =>
                    msg.id === loadingIndicatorId ? 
                    { ...msg, text: 'There was an error processing your selection. Please try again.' } : 
                    msg
                )
            );
        }
    };

    return (
        <div className="app-container">
            <div className="main-content">
                <div className="chat-window">
                    <ChatContainer
                        messages={messages}
                        prompt={prompt}
                        setPrompt={setPrompt}
                        handleSubmit={handleSubmit}
                        cancelChat={cancelChat}
                        streamingMessageId={streamingMessageId}
                        messagesEndRef={messagesEndRef}
                        shouldAutoScroll={shouldAutoScroll}
                        chatId=""
                        selectedFiles={selectedFiles}
                        setSelectedFiles={setSelectedFiles}
                        selectItinerary={selectItinerary}
                        renderMessages={() => (
                            <VirtualizedChatList messages={messages} />
                        )}
                    />
                </div>
            </div>
            <footer className="app-footer">
                <div className="footer-content">
                    <span>© 2025 Accede Concierge &mdash; an eShop company.</span>
                    <div className="footer-links">
                        <a href="#">Help</a>
                        <a href="#">Privacy Policy</a>
                        <a href="#">Terms of Service</a>
                    </div>
                </div>
            </footer>
        </div>
    );
};

export default App;
