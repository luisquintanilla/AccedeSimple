import React, { useEffect, ReactNode, RefObject, useState, useRef, ChangeEvent } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import remarkBreaks from 'remark-breaks';
import { Message, FileAttachment, CandidateItinerariesMessage, TripApprovalResultMessage } from '../types/ChatTypes';
import { TripOption } from '../types/TripTypes';
import { TripRequestStatus } from '../types/AdminTypes';

// Helper function to determine if a message is a progress type message
const isProgressMessage = (messageType: string): boolean => {
    return ['preference-updated', 'trip-request-updated', 'receipts-processed'].includes(messageType);
};

// Helper function to get status-specific icon
const getStatusIcon = (status: TripRequestStatus): JSX.Element => {
    switch (status) {
        case TripRequestStatus.Approved:
            return (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
            );
        case TripRequestStatus.Rejected:
            return (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
            );
        case TripRequestStatus.Cancelled:
            return (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
            );
        case TripRequestStatus.Pending:
        default:
            return (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
            );
    }
};

// Helper function to get status class name
const getStatusClassName = (status: TripRequestStatus): string => {
    switch (status) {
        case TripRequestStatus.Approved:
            return 'trip-status-approved';
        case TripRequestStatus.Rejected:
            return 'trip-status-rejected';
        case TripRequestStatus.Cancelled:
            return 'trip-status-cancelled';
        case TripRequestStatus.Pending:
        default:
            return 'trip-status-pending';
    }
};

// Helper function to get status title
const getStatusTitle = (status: TripRequestStatus): string => {
    switch (status) {
        case TripRequestStatus.Approved:
            return 'Trip Request Approved';
        case TripRequestStatus.Rejected:
            return 'Trip Request Rejected';
        case TripRequestStatus.Cancelled:
            return 'Trip Request Cancelled';
        case TripRequestStatus.Pending:
        default:
            return 'Trip Request Pending';
    }
};

interface ChatContainerProps {
    messages: Message[];
    prompt: string;
    setPrompt: (prompt: string) => void;
    handleSubmit: (e: React.FormEvent) => void;
    cancelChat: () => void;
    streamingMessageId: string | null;
    messagesEndRef: RefObject<HTMLDivElement | null>;
    shouldAutoScroll: boolean;
    renderMessages: () => ReactNode;
    chatId: string;
    selectedFiles: File[];
    setSelectedFiles: (files: File[]) => void;
    selectItinerary?: (messageId: string, optionId: string) => void;
}

const ChatContainer: React.FC<ChatContainerProps> = ({
    messages,
    prompt,
    setPrompt,
    handleSubmit,
    cancelChat,
    streamingMessageId,
    messagesEndRef,
    shouldAutoScroll,
    selectedFiles,
    setSelectedFiles,
    selectItinerary
}: ChatContainerProps) => {
    const [copiedMsgId, setCopiedMsgId] = useState<string | null>(null);
    const [canScrollUp, setCanScrollUp] = useState<boolean>(false);
    const [canScrollDown, setCanScrollDown] = useState<boolean>(false);
    const containerRef = useRef<HTMLDivElement | null>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);

    // Function to copy message text to clipboard
    const copyToClipboard = (text: string, msgId: string) => {
        navigator.clipboard.writeText(text).then(
            () => {
                setCopiedMsgId(msgId);
                // Reset copied state after 2 seconds
                setTimeout(() => setCopiedMsgId(null), 2000);
            },
            (err) => {
                console.error('Could not copy text: ', err);
            }
        );
    };

    // Function to handle itinerary selection
    const handleItinerarySelect = (messageId: string, optionId: string) => {
        if (selectItinerary) {
            selectItinerary(messageId, optionId);
        }
    };

    const renderTripOption = (option: TripOption, index: number, messageId: string) => {
        return (
            <div key={option.optionId || index} className="trip-option">
                <h3>Option {index + 1}: {option.description}</h3>
                <div className="trip-option-details">
                    {/* Flights section */}
                    <div className="trip-section flights">
                        <h4>Flights</h4>
                        {option.flights.map((flight, flightIdx) => (
                            <div key={flightIdx} className="flight-item">
                                <div className="flight-header">
                                    <strong>{flight.airline} {flight.flightNumber}</strong>
                                </div>
                                <div className="flight-route">
                                    {flight.origin} → {flight.destination}
                                </div>
                                <div className="detail-row">
                                    <span className="detail-label">Departure:</span>
                                    <span className="detail-value date-time">
                                        {new Date(flight.departureTime).toLocaleDateString()} {new Date(flight.departureTime).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}
                                    </span>
                                </div>
                                <div className="detail-row">
                                    <span className="detail-label">Arrival:</span>
                                    <span className="detail-value date-time">
                                        {new Date(flight.arrivalTime).toLocaleDateString()} {new Date(flight.arrivalTime).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}
                                    </span>
                                </div>
                                {flight.cabinClass && (
                                    <div className="detail-row">
                                        <span className="detail-label">Class:</span>
                                        <span className="detail-value"><strong>{flight.cabinClass}</strong></span>
                                    </div>
                                )}
                                <div className="detail-row">
                                    <span className="detail-label">Price:</span>
                                    <span className="detail-value price-tag">${flight.price.toFixed(2)}</span>
                                </div>
                            </div>
                        ))}
                    </div>
                    {/* Hotel section if available */}
                    {option.hotel && (
                        <div className="trip-section hotel">
                            <h4>Hotel</h4>
                            <div className="detail-row">
                                <span className="detail-label">Property:</span>
                                <span className="detail-value"><strong>{option.hotel.propertyName}</strong></span>
                            </div>
                            <div className="detail-row">
                                <span className="detail-label">Chain:</span>
                                <span className="detail-value">{option.hotel.chain}</span>
                            </div>
                            <div className="detail-row">
                                <span className="detail-label">Address:</span>
                                <span className="detail-value">{option.hotel.address}</span>
                            </div>
                            <div className="divider"></div>
                            <div className="detail-row">
                                <span className="detail-label">Check-in:</span>
                                <span className="detail-value date-time">
                                    {new Date(option.hotel.checkIn).toLocaleDateString()}
                                </span>
                            </div>
                            <div className="detail-row">
                                <span className="detail-label">Check-out:</span>
                                <span className="detail-value date-time">
                                    {new Date(option.hotel.checkOut).toLocaleDateString()}
                                </span>
                            </div>
                            <div className="detail-row">
                                <span className="detail-label">Duration:</span>
                                <span className="detail-value"><strong>{option.hotel.nightCount} nights</strong></span>
                            </div>
                            <div className="detail-row">
                                <span className="detail-label">Room Type:</span>
                                <span className="detail-value">{option.hotel.roomType}</span>
                            </div>
                            <div className="divider"></div>
                            <div className="detail-row">
                                <span className="detail-label">Price per night:</span>
                                <span className="detail-value price-tag">${option.hotel.pricePerNight.toFixed(2)}</span>
                            </div>
                            <div className="detail-row">
                                <span className="detail-label">Total price:</span>
                                <span className="detail-value price-tag">${option.hotel.totalPrice.toFixed(2)}</span>
                            </div>
                            {option.hotel.breakfastIncluded && (
                                <div className="detail-row">
                                    <span className="detail-label">Includes:</span>
                                    <span className="detail-value">
                                        <span className="check-icon">✓</span> Breakfast
                                    </span>
                                </div>
                            )}
                        </div>
                    )}
                    {/* Car rental section if available */}
                    {option.car && (
                        <div className="trip-section car-rental">
                            <h4>Car Rental</h4>
                            <div className="detail-row">
                                <span className="detail-label">Company:</span>
                                <span className="detail-value"><strong>{option.car.company}</strong></span>
                            </div>
                            <div className="detail-row">
                                <span className="detail-label">Car type:</span>
                                <span className="detail-value">{option.car.carType}</span>
                            </div>
                            <div className="divider"></div>
                            <div className="detail-row">
                                <span className="detail-label">Pick-up:</span>
                                <span className="detail-value">
                                    <span className="location-icon">📍</span> 
                                    {option.car.pickupLocation}
                                </span>
                            </div>
                            <div className="detail-row">
                                <span className="detail-label">Time:</span>
                                <span className="detail-value date-time">
                                    {new Date(option.car.pickupTime).toLocaleDateString()} {new Date(option.car.pickupTime).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}
                                </span>
                            </div>
                            <div className="divider"></div>
                            <div className="detail-row">
                                <span className="detail-label">Drop-off:</span>
                                <span className="detail-value">
                                    <span className="location-icon">📍</span> 
                                    {option.car.dropoffLocation}
                                </span>
                            </div> 
                            <div className="detail-row">
                                <span className="detail-label">Time:</span>
                                <span className="detail-value date-time">
                                    {new Date(option.car.dropoffTime).toLocaleDateString()} {new Date(option.car.dropoffTime).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}
                                </span>
                            </div>
                            <div className="divider"></div>
                            <div className="detail-row">
                                <span className="detail-label">Daily rate:</span>
                                <span className="detail-value price-tag">${option.car.dailyRate.toFixed(2)}</span>
                            </div>
                            <div className="detail-row">
                                <span className="detail-label">Total price:</span>
                                <span className="detail-value price-tag">${option.car.totalPrice.toFixed(2)}</span>
                            </div>
                            {option.car.unlimitedMileage && (
                                <div className="detail-row">
                                    <span className="detail-label">Features:</span>
                                    <span className="detail-value">
                                        <span className="check-icon">✓</span> Unlimited mileage
                                    </span>
                                </div>
                            )}
                        </div>
                    )}
                </div>
                <div className="trip-option-total">
                    <p><strong>Total Cost: <span className="price-tag">${option.totalCost.toFixed(2)}</span></strong></p>
                    <button 
                        className="select-option-button"
                        onClick={() => handleItinerarySelect(messageId, option.optionId)}
                    >
                        Select this itinerary
                    </button>
                </div>
            </div>
        );
    };
    
    // Function to render trip approval result message
    const renderTripApprovalResult = (msg: TripApprovalResultMessage) => {
        const { result } = msg;
        const statusClass = getStatusClassName(result.status);
        const statusIcon = getStatusIcon(result.status);
        const statusTitle = getStatusTitle(result.status);
        
        return (
            <div className={`message-content ${statusClass}`}>
                <div className="status-icon">
                    {statusIcon}
                </div>
                <div className="status-content">
                    <h3 className="status-title">{statusTitle}</h3>
                    <p className="status-details">
                        {result.approvalNotes && `${result.approvalNotes} • `}
                        Processed on {new Date(result.processedTime).toLocaleString()}
                    </p>
                </div>
            </div>
        );
    };
    
    // Function to render messages with unified approach
    const renderMessages = () => {
        return messages.map(msg => {
            const isProgress = isProgressMessage(msg.type);
            
            return (
                <div 
                    key={msg.id} 
                    className={`message ${msg.role} ${isProgress ? 'progress-message' : ''} ${
                        msg.type === 'trip-approval-result' ? getStatusClassName((msg as TripApprovalResultMessage).result.status) : ''
                    }`}
                    data-type={msg.type}
                >
                    <div className="message-container">
                        {msg.type === 'trip-approval-result' ? (
                            renderTripApprovalResult(msg as TripApprovalResultMessage)
                        ) : (
                            <div className="message-content">
                                <ReactMarkdown 
                                    remarkPlugins={[remarkGfm, remarkBreaks]}
                                >
                                    {msg.text}
                                </ReactMarkdown>
                                
                                {/* Render trip options only for candidate itineraries type */}
                                {msg.type === 'candidate-itineraries' && (msg as CandidateItinerariesMessage).options && (
                                    <div className="trip-options-container">
                                        {(msg as CandidateItinerariesMessage).options.map((option, index) => 
                                            renderTripOption(option, index, msg.id)
                                        )}
                                    </div>
                                )}
                                
                                {renderAttachments(msg.attachments)}
                                
                                {/* Don't show copy button for preference messages */}
                                {!isProgress && (
                                    <button 
                                        className={`copy-message-button ${copiedMsgId === msg.id ? 'copied' : ''}`}
                                        onClick={() => copyToClipboard(msg.text, msg.id)}
                                        aria-label="Copy message"
                                        title="Copy to clipboard"
                                    >
                                        {copiedMsgId === msg.id ? (
                                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                                <polyline points="20 6 9 17 4 12"></polyline>
                                            </svg>
                                        ) : (
                                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                                <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                                                <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                                            </svg>
                                        )}
                                    </button>
                                )}
                            </div>
                        )}
                    </div>
                </div>
            );
        });
    };
    
    // Check if container can scroll and show/hide shadows accordingly
    const checkScroll = () => {
        if (messagesEndRef.current) {
            const { scrollTop, scrollHeight, clientHeight } = messagesEndRef.current;
            
            // Show top shadow if we're not at the top (more sensitive threshold)
            setCanScrollUp(scrollTop > 5);
            
            // Show bottom shadow if we're not at the bottom (more sensitive threshold)
            setCanScrollDown(scrollTop + clientHeight < scrollHeight - 5);
        }
    };

    // Handle file selection
    const handleFileSelect = (e: ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files.length > 0) {
            // Convert FileList to array and filter for only image files
            const fileArray = Array.from(e.target.files).filter(
                file => file.type.startsWith('image/')
            );
            
            if (fileArray.length > 0) {
                setSelectedFiles([...selectedFiles, ...fileArray]);
            }
        }
    };

    // Handle click on attachment button
    const handleAttachmentClick = () => {
        if (fileInputRef.current) {
            fileInputRef.current.click();
        }
    };

    // Remove file from selected files
    const removeSelectedFile = (indexToRemove: number) => {
        setSelectedFiles(selectedFiles.filter((_, index) => index !== indexToRemove));
    };

    // Scroll only if near the bottom
    useEffect(() => {
        if (shouldAutoScroll && messagesEndRef.current) {
            messagesEndRef.current.scrollIntoView({ behavior: 'smooth' });
        }
    }, [messages, shouldAutoScroll, messagesEndRef]);

    // Setup scroll listener
    useEffect(() => {
        const currentRef = messagesEndRef.current;
        
        if (currentRef) {
            // Initial check
            checkScroll();
            
            // Add scroll listener
            currentRef.addEventListener('scroll', checkScroll);
            
            // Check after content changes
            const observer = new MutationObserver(checkScroll);
            observer.observe(currentRef, { childList: true, subtree: true });
            
            return () => {
                currentRef.removeEventListener('scroll', checkScroll);
                observer.disconnect();
            };
        }
    }, []);

    // Check scroll state on content changes
    useEffect(() => {
        checkScroll();
    }, [messages]);

    // Render attachments in message
    const renderAttachments = (attachments?: FileAttachment[]) => {
        if (!attachments || attachments.length === 0) return null;
        
        return (
            <div className="message-attachments">
                {attachments.map((attachment, index) => (
                    <div key={index} className="message-image-attachment">
                        <a href={attachment.uri} target="_blank" rel="noopener noreferrer">
                            <img 
                                src={attachment.uri} 
                                className="attached-image"
                            />
                        </a>
                    </div>
                ))}
            </div>
        );
    };

    return (
        <div 
            ref={containerRef} 
            className={`chat-container ${canScrollUp ? 'can-scroll-up' : ''} ${canScrollDown ? 'can-scroll-down' : ''}`}
        >
            {/* Top shadow indicator */}
            <div className={`scroll-shadow-top ${canScrollUp ? 'visible' : ''}`} />
            
            <div ref={messagesEndRef} className="messages-container" onScroll={checkScroll}>
                {renderMessages()}
            </div>
            
            {/* Form container with positioned shadow */}
            <div className="form-container">
                {/* Bottom shadow indicator positioned above the form */}
                <div className={`scroll-shadow-bottom ${canScrollDown ? 'visible' : ''}`} />
                
                {/* Show selected file previews */}
                {selectedFiles.length > 0 && (
                    <div className="selected-files-container">
                        {selectedFiles.map((file, index) => (
                            <div key={index} className="selected-file-preview">
                                <img 
                                    src={URL.createObjectURL(file)} 
                                    alt={file.name}
                                    className="file-preview-thumbnail" 
                                />
                                <button 
                                    className="remove-file-button"
                                    onClick={() => removeSelectedFile(index)}
                                    aria-label="Remove file"
                                >
                                    ×
                                </button>
                            </div>
                        ))}
                    </div>
                )}
                
                <form onSubmit={handleSubmit} className="message-form" autoComplete="off">
                    {/* Hidden file input */}
                    <input
                        type="file"
                        ref={fileInputRef}
                        onChange={handleFileSelect}
                        style={{ display: 'none' }}
                        accept="image/*"
                        multiple
                    />
                    
                    {/* Attachment button */}
                    <button 
                        type="button" 
                        onClick={handleAttachmentClick}
                        disabled={streamingMessageId ? true : false}
                        className="attachment-button"
                        title="Attach image"
                    >
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48"></path>
                        </svg>
                    </button>
                    
                    <input
                        type="text"
                        value={prompt}
                        onChange={e => setPrompt(e.target.value)}
                        placeholder="How can I help you with your travel plans?"
                        disabled={streamingMessageId ? true : false}
                        className="message-input"
                        autoComplete="off"
                        name="message-input"
                    />
                    {streamingMessageId ? (
                        <button type="button" onClick={cancelChat} className="message-button">
                            Stop
                        </button>
                    ) : (
                        <button type="submit" disabled={streamingMessageId ? true : false} className="message-button">
                            Send
                        </button>
                    )}
                </form>
            </div>
        </div>
    );
};

export default ChatContainer;
